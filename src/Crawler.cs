using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MemriseBot {
    /// <summary>
    /// Responsible for crawling the Memrise website.
    /// </summary>
    public class Crawler {

        private const string CookiesPath = "../../../data/app.memrise.com.cookies.json", 
            SelectorsPath = "../../../data/selectors.json",
            MemriseURL = "https://app.memrise.com/dashboard/scenarios";
        private const int GeneralTimeout = 6;
        private const double QuestionTimeout = .5;

        private Dictionary<string, string> ?selectors;
        private ChromeDriver driver; 
        private Translator translator = new Translator();

        /// <summary>
        /// For ease of decoding the cookies json.
        /// </summary>
        class Cookie {
            public string name { get; set; }
            public string value { get; set; }
            public string domain { get; set; }
            public string path { get; set; }
            public float expires { get; set; }
            public bool httpOnly { get; set; }
            public bool secure { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Crawler"/> class, by opening a new window.
        /// </summary>
        public Crawler() { 
            // turn off Selenium's logs
            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;
            driver = new ChromeDriver(service);

            // timeout
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(GeneralTimeout);

            // selectors
            selectors = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(SelectorsPath));
        }

        /// <summary>
        /// Logs in to the Memrise website with cookies.
        /// </summary>
        public void Login() {
            driver.Navigate().GoToUrl(MemriseURL);

            List<Cookie> ?cookies = JsonConvert.DeserializeObject<List<Cookie>>(File.ReadAllText(CookiesPath));
            foreach (Cookie cookie in cookies!) { 
                driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(
                    cookie.name, 
                    cookie.value, 
                    cookie.domain, 
                    cookie.path, 
                    DateTime.Now.AddSeconds(cookie.expires)
                ));
            }

            driver.Navigate().GoToUrl(MemriseURL);
        }

        /// <summary>
        /// Learns a new word.
        /// </summary>
        private void Learn() {
            // multiple possible formats
            string word = null, translation = null;

            try { 
                word = driver.FindElement(By.CssSelector(selectors!["learn-word"])).Text;
                translation = driver.FindElement(By.CssSelector(selectors!["learn-translation"])).Text; 
            }
            catch (NoSuchElementException) {}

            try { 
                word = driver.FindElement(By.CssSelector(selectors!["learn-word-video"])).Text; 
                translation = driver.FindElement(By.CssSelector(selectors!["learn-translation-video"])).Text;
            }
            catch (NoSuchElementException) {}
                    
            try { 
                word = driver.FindElement(By.CssSelector(selectors!["learn-word-correction"])).Text; 
                translation = driver.FindElement(By.CssSelector(selectors!["learn-translation-correction"])).Text;
            }
            catch (NoSuchElementException) {}

            // program started before the new page loaded
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(translation)) {
                Console.Error.WriteLine("Going too fast."); 
                return;
            } 

            translator.Learn(word, translation);

            // move on by pressing continue or next
            try {
                driver.FindElement(By.CssSelector(selectors!["learn-continue"])).Click();
                return;
            }
            catch (NoSuchElementException) {}

            try { driver.FindElement(By.CssSelector(selectors!["learn-next"])).Click(); }
            catch (NoSuchElementException) {}
        }

        /// <summary>
        /// Answers a multiple choice question.
        /// </summary>
        private void MultipleChoice() {
            string word = driver.FindElement(By.CssSelector(selectors!["exercise-word"])).Text;
            ReadOnlyCollection<IWebElement> optionDivs = driver.FindElements(By.CssSelector(selectors!["multiple-choice-options"]));
            string ?translation = translator.Translate(word);
            
            // if the word is unknown, learn
            if (translation == null) {
                driver.FindElement(By.CssSelector(selectors!["exercise-I-don't-know"])).Click();
                return;
            }

            // pick a choice
            foreach (IWebElement option in optionDivs) {
                if (option.Text.Split("\n")[1] != translation) continue;
                option.Click();
                break;
            }
        }

        /// <summary>
        /// Answers a type question.
        /// </summary>
        private void TypeQuestion() {
            string word = driver.FindElement(By.CssSelector(selectors!["exercise-word"])).Text;
            IWebElement input = driver.FindElement(By.CssSelector(selectors!["typing-input"]));
            string ?translation = translator.Translate(word);

            // if the word is unknown, learn
            if (translation == null) {
                driver.FindElement(By.CssSelector(selectors!["exercise-I-don't-know"])).Click();
                return;
            }

            // type the translation
            input.SendKeys(translation);

            // potentially press enter
            try { input.SendKeys(Keys.Enter); }
            catch (ElementNotInteractableException) {}
        }

        /// <summary>
        /// Completes an exercise page, by first identifying what type of exercise it is.
        /// </summary>
        /// <remarks>Video/audio based exercises not supported.</remarks>
        private void CompleteExercise() {
            string ?prompt = null;
            try { prompt = driver.FindElement(By.CssSelector(selectors!["exercise-prompt"])).Text; }
            catch (NoSuchElementException) {}

            if (prompt == null) {
                Learn();
                return;
            }

            if (prompt == "Pick the correct answer") {
                MultipleChoice();
                return;
            }

            if (prompt == "Type the correct translation") {
                TypeQuestion();
                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Plays a session.
        /// </summary>
        /// <returns>The points earned in the session.</returns>
        private int PlaySession() {
            // potentially close ad button
            try { driver.FindElement(By.CssSelector(selectors!["commit-ad-x"])).Click(); }
            catch (NoSuchElementException) {}

            // faster timeout for questions
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(QuestionTimeout);

            while (true) {
                CompleteExercise();

                // potentially close out of messages
                try { driver.FindElement(By.CssSelector(selectors!["subscription-maybe-later"])).Click(); }
                catch (NoSuchElementException) {}
                try { driver.FindElement(By.CssSelector(selectors!["streak-awesome"])).Click(); }
                catch (NoSuchElementException) {}

                // session complete
                try {
                    driver.FindElement(By.CssSelector(selectors!["scenario-summary"]));
                    break;
                }
                catch (NoSuchElementException) {}
            }

            // slower
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(GeneralTimeout);

            // loading time
            Thread.Sleep(GeneralTimeout * 1000 * 3);

            // get points "earned"
            string pointText = driver.FindElement(By.CssSelector(selectors!["session-points"])).Text;
            int points = int.Parse(pointText.Split(" ")[0]);

            // press continue
            driver.FindElement(By.CssSelector(selectors!["scenario-summary"])).Click();

            driver.FindElement(By.CssSelector(selectors!["summary-continue"])).Click();
            Thread.Sleep(GeneralTimeout * 1000);

            // potentially close level up
            try { 
                driver.FindElement(By.CssSelector(selectors!["level-up-continue"])).Click();
                driver.FindElement(By.CssSelector(selectors!["next-stage-continue"])).Click();
            }
            catch (NoSuchElementException) {}

            return points;
        }

        /// <summary>
        /// Picks a "Learn words" session.
        /// </summary>
        private void PickSession() {
            string title = driver.FindElement(By.CssSelector(selectors!["session-title"])).Text;
            if (title == "Hear my words") {
                driver.FindElement(By.CssSelector(selectors!["left-arrow"])).Click();
            }
            if (title == "Use my words") {
                driver.FindElement(By.CssSelector(selectors!["right-arrow"])).Click();
            }

            Thread.Sleep(GeneralTimeout * 1000);
            driver.FindElement(By.CssSelector(selectors!["start"])).Click();
            Thread.Sleep(GeneralTimeout * 1000 * 2);

            // throwing error means the scenario is available
            try { 
                driver.FindElement(By.CssSelector(selectors!["skip-scenario"])).Click();
                PickSession();
            }
            catch (NoSuchElementException) {}
        }
    
        /// <summary>
        /// Plays for points until the goal is reached.
        /// </summary>
        /// <param name="goal">The number of points to reach.</param>
        public void PlayForPoints(int goal) {
            int pointsLeft = goal;

            // potentially close pop ups
            try { driver.FindElement(By.CssSelector(selectors!["ad-back"])).Click(); }
            catch (NoSuchElementException) {}
 
            while (pointsLeft > 0) {
                PickSession();
                int earned = PlaySession();
                Console.WriteLine("Earned " + earned + " points.");
                pointsLeft -= earned;
            }
        }

        public void Close() {
            driver.Quit();
        }
    }
}