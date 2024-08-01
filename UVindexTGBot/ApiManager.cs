using System.Text.Json.Serialization;
using System.Text.Json;

namespace UVindexTGBot
{
    internal class ApiManager
    {
        private const string ApiUrlTemplate = "{0}?lat={1}&lon={2}&dt={3}&appid={4}";

        public string? UvApiUrl { get; set; }
        internal string? ApiKey;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Uvi { get; set; }
        public long Sunrise { get; set; }
        public long Sunset { get; set; }
        internal string? BotToken { get; set; }

        internal async Task GetWeatherDataFromApi()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string requestUrl = string.Format(ApiUrlTemplate, UvApiUrl, Latitude, Longitude, currentTime, ApiKey);

            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API request failed with status code: {response.StatusCode}");
                return;
            }

            string result = await response.Content.ReadAsStringAsync();
            try
            {
                var weatherData = JsonSerializer.Deserialize<WeatherData>(result);

                if (weatherData?.Data != null && weatherData.Data.Length > 0)
                {
                    var dataNode = weatherData.Data[0];
                    Uvi = dataNode.Uvi;
                    Sunrise = dataNode.Sunrise;
                    Sunset = dataNode.Sunset;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse JSON response: {ex.Message}");
            }
        }

        private sealed class WeatherData
        {
            [JsonPropertyName("data")]
            public WeatherInfo[]? Data { get; set; }
        }

        private sealed class WeatherInfo
        {
            [JsonPropertyName("uvi")]
            public float Uvi { get; set; }

            [JsonPropertyName("sunrise")]
            public long Sunrise { get; set; }

            [JsonPropertyName("sunset")]
            public long Sunset { get; set; }
        }
    }
}

