using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MemriseBot.src {
    /// <summary>
    /// Responsible for crawling the Memrise website.
    /// </summary>
    public class Crawler {

        private const string CookiesPath = "../../../data/app.memrise.com.cookies.json";
        private Dictionary<string, string> selectors = new Dictionary<string, string> {
            { "back", "#__next > div > div > div.sc-36oahp-0.inakBl > div.sc-fbt2ce-0.blplTm > a:nth-child(2) > div" },
            { "start", "#floatingBottomPortalRoot > div > div > div > div > div > div.slick-list > div > div.slick-slide.slick-active.slick-current > div > div > div.sc-1txhy2h-0.iVaTfJ > button > div" }
        };
        private ChromeDriver ?driver;

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
            driver = new ChromeDriver(); 
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
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
        /// Plays a game.
        /// </summary>
        public void Play() {
            driver!.FindElement(By.CssSelector(selectors.)).Click();
        }
    }
}