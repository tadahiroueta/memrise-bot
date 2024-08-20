using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MemriseBot {
    /// <summary>
    /// Responsible for crawling the Memrise website.
    /// </summary>
    public class Crawler {

        private const string CookiesPath = "../../../data/app.memrise.com.cookies.json", 
            SelectorsPath = "../../../data/selectors.json";
        private Dictionary<string, string> ?selectors;

        private ChromeDriver ?driver = new ChromeDriver(); 
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
            // timeout
            driver!.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // selectors
            selectors = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(SelectorsPath));
        }

        /// <summary>
        /// Adds cookies from json as credentials for login.
        /// </summary>
        private void AddCookies() {
            List<Cookie> ?cookies = JsonConvert.DeserializeObject<List<Cookie>>(File.ReadAllText(CookiesPath));
            foreach (Cookie cookie in cookies!) { 
                driver!.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(
                    cookie.name, 
                    cookie.value, 
                    cookie.domain, 
                    cookie.path, 
                    DateTime.Now.AddSeconds(cookie.expires)
                ));
            }
        }

        /// <summary>
        /// Logs in to the Memrise website.
        /// </summary>
        public void Login() {
            driver!.Navigate().GoToUrl("https://app.memrise.com/login/");
            AddCookies();
            driver!.Navigate().GoToUrl("https://app.memrise.com/dashboard/scenarios");
        }

        /// <summary>
        /// Learns a new word.
        /// </summary>
        private void Learn() {
            string word = driver!.FindElement(By.CssSelector(selectors!["learn-word"])).Text,
                translation = driver!.FindElement(By.CssSelector(selectors!["learn-translation"])).Text;

            translator.Learn(word, translation);

            // move on by pressing continue or next
            try {
                driver!.FindElement(By.CssSelector(selectors!["learn-continue"])).Click();
                return;
            }
            catch (NoSuchElementException) {}

            try { driver!.FindElement(By.CssSelector(selectors!["learn-next"])).Click(); }
            catch (NoSuchElementException) {}
        }

        /// <summary>
        /// Answers a multiple choice question.
        /// </summary>
        private void MultipleChoice() {
            string word = driver!.FindElement(By.CssSelector(selectors!["multiple-choice-word"])).Text;
            ReadOnlyCollection<IWebElement> optionDivs = driver!.FindElements(By.CssSelector(selectors!["multiple-choice-options"]));
            
            string ?translation = translator.Translate(word);
            
            // if the word is unknown, learn
            if (translation == null) {
                driver!.FindElement(By.CssSelector(selectors!["multiple-choice-I-don't-know"])).Click();
                return;
            }

            // pick a choice
            foreach (IWebElement option in optionDivs) {
                if (option.Text != translation) continue;
                option.Click();
                return;
            }
        }

        /// <summary>
        /// Answers a type question.
        /// </summary>
        private void TypeQuestion() {
            string word = driver!.FindElement(By.CssSelector(selectors!["type-word"])).Text;
            IWebElement input = driver!.FindElement(By.CssSelector(selectors!["typing-input"]));

            string ?translation = translator.Translate(word);

            // TODO

        }

        /// <summary>
        /// Completes an exercise page, by first identifying what type of exercise it is.
        /// </summary>
        /// <remarks>Video/audio based exercises not supported.</remarks>
        private void CompleteExercise() {
            string prompt = null;
            try { prompt = driver!.FindElement(By.CssSelector(selectors!["exercise-prompt"])).Text; }
            catch (NoSuchElementException) {}

            if (prompt == null) {
                Learn();
                return;
            }

            if (prompt == "Pick the correct translation") {
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
        /// Plays a game.
        /// </summary>
        public void Play() {
            // potentially close ad button
            try { driver!.FindElement(By.CssSelector(selectors!["ad-back"])).Click(); }
            catch (NoSuchElementException) {}

            // start game
            driver!.FindElement(By.CssSelector(selectors!["start"])).Click();

            // potentially close ad button
            try { driver!.FindElement(By.CssSelector(selectors["commit-ad-x"])).Click(); }
            catch (NoSuchElementException) {}

            // TODO
            for (int i = 0; i < 10; i++) { CompleteExercise(); }
        }
    }
}