using Telegram.Bot;

namespace UVindexTGBot
{
    public class UvUpdateScheduler : IDisposable
    {
        private readonly ITelegramBotClient botClient;
        private readonly ApiManager apiManager;
        private Timer? timer;
        private bool disposed;

        internal long ChatId { get; set; }

        public UvUpdateScheduler(ITelegramBotClient botClient, ApiManager api) 
        {
            this.botClient = botClient;
            apiManager = api;
        }

        internal void ScheduleUvUpdates(TimeSpan interval, CancellationToken cancellationToken)
        {
            timer = new Timer(async state => await SendUvUpdateAsync(cancellationToken), null, TimeSpan.Zero, interval);
        }

        internal async Task SendUvUpdateAsync(CancellationToken cancellationToken, bool isUserRequest = false)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await apiManager.GetWeatherDataFromApi(cancellationToken);
            var uvi = apiManager.Uvi;

            if (currentTime >= apiManager.Sunrise && currentTime <= apiManager.Sunset)
            {
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: $"Current UV index is {uvi}",
                    cancellationToken: cancellationToken
                );
            }
            else if (isUserRequest)
            {
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "It's nighttime, and the UV index is typically 0.",
                    cancellationToken: cancellationToken
                );
            }
        }

        internal void CancelUvUpdates(CancellationToken cancellationToken)
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
