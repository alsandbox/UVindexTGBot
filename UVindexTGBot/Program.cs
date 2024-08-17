using System.Text.Json;
using File = System.IO.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UVindexTGBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading bot token...");
            string botToken = GetTokenFromArgsOrEnv(args, "BOT_TOKEN", 0);

            Console.WriteLine("Reading API key...");
            string apiKey = GetTokenFromArgsOrEnv(args, "API_KEY", 1);

            string fileName = "config.json";

            try
            {
                string jsonString = await File.ReadAllTextAsync(fileName);
                ApiManager? api = JsonSerializer.Deserialize<ApiManager>(jsonString);

                if (api is null)
                {
                    Console.WriteLine("Failed to deserialize API manager from config file. API is null.");
                    return;
                }

                api.ApiKey = apiKey;
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

        static string GetTokenFromArgsOrEnv(string[] args, string envVarName, int argPosition)
        {
            string? token = null;

            if (args.Length > argPosition)
            {
                token = args[argPosition];
            }
            else
            {
                token = Environment.GetEnvironmentVariable(envVarName);
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"The {envVarName} must be provided via command-line arguments or the {envVarName} environment variable.");
            }

            return token;
        }

    }
}
