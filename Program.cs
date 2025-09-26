using System;

class Program
{
    static void Main()
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Stopping (Ctrl+C)...");
            e.Cancel = true; // prevent immediate termination
        };

        Console.WriteLine("Starting fishing bot in 5 seconds...");
        System.Threading.Thread.Sleep(5000);

        var config = ConfigData.Load("config.json");
        var bot = new FishingBot(config);

        // Handle Ctrl+C by stopping the bot
        Console.CancelKeyPress += (s, e) =>
        {
            bot.Stop();
        };

        bot.Run();
    }
}