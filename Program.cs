using MemriseBot.src;

class Program {
    static void Main() {
        Crawler crawler = new Crawler();
        crawler.Login();
        crawler.Play();
    }
}