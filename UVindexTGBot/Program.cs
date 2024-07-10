using System.Text.Json;
using File = System.IO.File;

namespace UVindexTGBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Please, enter the bot token: ");
            string? botToken = Console.ReadLine(); 
            string fileName = "config.json";

            string jsonString = await File.ReadAllTextAsync(fileName);
            ApiManager? api = JsonSerializer.Deserialize<ApiManager>(jsonString);

            if (api == null) return;

            api.BotToken = botToken;

            BotManager botManager = new(botToken, api);

            await botManager.StartReceiving();
        }
    }
}
