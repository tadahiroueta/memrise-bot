using MemriseBot;

class Program {
    static void Main() {
        // testing Crawler
        Crawler crawler = new Crawler();
        crawler.Login();
        crawler.Play();

        // testing Translator
        // Translator translator = new Translator();
        // translator.learn("hola", "hello");
        // translator.learn("adios", "goodbye");
        // translator.learn("gracias", "thank you");
        // translator.learn("por favor", "please");
        // // print
        // Console.WriteLine(translator.translate("hola"));
        // Console.WriteLine(translator.translateBack("hello"));
        // Console.WriteLine(translator.translate("adios"));
        // Console.WriteLine(translator.translateBack("goodbye"));
    }
}