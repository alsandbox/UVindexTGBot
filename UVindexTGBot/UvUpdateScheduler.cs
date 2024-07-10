using Telegram.Bot;

namespace UVindexTGBot
{
    internal class UvUpdateScheduler : IDisposable
    {
        private readonly TelegramBotClient botClient;
        private readonly ApiManager api;
        private readonly CancellationToken cancellationToken;
        private Timer? timer;
        private bool disposed;

        internal long ChatId { get; set; }

        internal UvUpdateScheduler(TelegramBotClient botClient, ApiManager api, CancellationToken cancellationToken) 
        {
            this.botClient = botClient;
            this.api = api;
            this.cancellationToken = cancellationToken;
        }

        internal void ScheduleUvUpdates(TimeSpan interval)
        {
            timer = new Timer(async state => await SendUvUpdateAsync(), null, TimeSpan.Zero, interval);
        }

        internal async Task SendUvUpdateAsync()
        {
            await api.GetUvFromApi();
            var uvi = api.Uvi;

            await botClient.SendTextMessageAsync(
                chatId: ChatId,
                text: $"Current UV index is {uvi}",
                cancellationToken: cancellationToken
            );
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
