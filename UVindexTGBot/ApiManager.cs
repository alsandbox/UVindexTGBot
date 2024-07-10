using System.Text.Json.Nodes;

namespace UVindexTGBot
{
    internal class ApiManager
    {
        public string? UvApiUrl { get; set; }
        internal string? ApiKey;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        private readonly long time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public float Uvi { get; set; }
        internal string? BotToken { get; set; }

        internal async Task GetUvFromApi()
        {
            Console.Write("Please, enter the API key: ");
            ApiKey = Console.ReadLine();
            string allApi = $"{UvApiUrl}?lat={Latitude}&lon={Longitude}&dt={time}&appid={ApiKey}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(allApi);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();

                JsonNode forecastNode = JsonNode.Parse(result)!;
                JsonNode dataArray = forecastNode!["data"]!;
                JsonNode uvIndex = dataArray[0]!;
                Uvi = uvIndex["uvi"]!.GetValue<float>();
            }
        }
    }
}

