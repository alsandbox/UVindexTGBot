using Telegram.Bot;

namespace UVindexTGBot
{
    internal class UvUpdateScheduler : IDisposable
    {
        private readonly TelegramBotClient botClient;
        private readonly ApiManager apiManager;
        private readonly CancellationToken cancellationToken;
        private Timer? timer;
        private bool disposed;

        internal long ChatId { get; set; }

        internal UvUpdateScheduler(TelegramBotClient botClient, ApiManager api, CancellationToken cancellationToken) 
        {
            this.botClient = botClient;
            apiManager = api;
            this.cancellationToken = cancellationToken;
        }

        internal void ScheduleUvUpdates(TimeSpan interval)
        {
            timer = new Timer(async state => await SendUvUpdateAsync(), null, TimeSpan.Zero, interval);
        }

        internal async Task SendUvUpdateAsync()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await apiManager.GetWeatherDataFromApi();
            var uvi = apiManager.Uvi;

            if (currentTime >= apiManager.Sunrise && currentTime <= apiManager.Sunset)
            {
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: $"Current UV index is {uvi}",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "It's nighttime, and the UV index is typically 0.",
                    cancellationToken: cancellationToken
                );
            }
        }

        internal void CancelUvUpdates()
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);

            botClient.SendTextMessageAsync(
                chatId: ChatId,
            text: "UV index updates have been cancelled.",
                cancellationToken: cancellationToken
            );
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                timer?.Dispose();
            }

            disposed = true;
        }
    }
}
