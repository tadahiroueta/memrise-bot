using MemriseBot;

class Program {
    static void Main() {
        Crawler crawler = new Crawler();
        crawler.Login();

        // get input
        int ?goal = null;
        while (goal == null) {
            Console.WriteLine("How many points would you like? ");
            try {
                int input = int.Parse(Console.ReadLine()!);
                if (input < 0) throw new Exception();
                goal = input;
            } 
            catch { Console.Error.WriteLine("Invalid input."); }
        }

        crawler.PlayForPoints(goal.Value);
    }
}