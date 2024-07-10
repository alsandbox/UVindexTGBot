using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace UVindexTGBot
{
    internal class BotManager : IDisposable
    {
        private readonly TelegramBotClient botClient;
        private readonly CancellationTokenSource cts;
        private readonly MessageHandler messageHandler;
        private bool disposed;

        internal BotManager(string botToken, ApiManager api)
        {
            botClient = new TelegramBotClient(botToken);
            cts = new CancellationTokenSource();
            LocationService locationService = new LocationService(botClient, api, cts.Token);
            UvUpdateScheduler uvUpdateScheduler = new UvUpdateScheduler(botClient, api, cts.Token);
            messageHandler = new MessageHandler(botClient, locationService, uvUpdateScheduler, cts.Token);
        }

        public async Task StartReceiving()
        {
            Console.WriteLine($"Bot has started");

            try
            {
                await ListenForMessagesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in StartReceiving: {ex.Message}");
            }

            Console.WriteLine("Bot is stopping...");
        }

        private async Task ListenForMessagesAsync()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            botClient.StartReceiving(
                updateHandler: messageHandler.HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions
            );

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Bot receiving has been cancelled.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Bot receiving operation has been cancelled.");
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Polling error: {exception.Message}");
            return Task.CompletedTask;
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
                cts?.Dispose();
            }

            disposed = true;
        }
    }
}
