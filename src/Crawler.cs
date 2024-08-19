using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;

namespace MemriseBot.src {
    /// <summary>
    /// Responsible for crawling the Memrise website.
    /// </summary>
    public class Crawler {

        private const string CookiesPath = "../../../data/app.memrise.com.cookies.json";
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
        public Crawler() { driver = new ChromeDriver(); }

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
    }
}