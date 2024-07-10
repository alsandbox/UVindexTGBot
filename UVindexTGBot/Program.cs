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
            if (string.IsNullOrWhiteSpace(botToken))
            {
                Console.WriteLine("Bot token cannot be empty.");
                return;
            }

            string fileName = "config.json";

            try
            {
                string jsonString = await File.ReadAllTextAsync(fileName);
                ApiManager? api = JsonSerializer.Deserialize<ApiManager>(jsonString);

                if (api == null)
                {
                    Console.WriteLine("Failed to deserialize API manager from config file. API is null.");
                    return;
                }

                api.BotToken = botToken;

                BotManager botManager = new(botToken, api);

                await botManager.StartReceiving();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Config file '{fileName}' not found.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse config file. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

    }
}
