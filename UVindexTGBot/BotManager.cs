using Telegram.Bot;
using Telegram.Bot.Types;

namespace UVindexTGBot
{
    public class BotManager : IDisposable
    {
        private readonly ITelegramBotClient botClient;
        private readonly MessageHandler messageHandler;
        private bool disposed;

        public BotManager(ITelegramBotClient botClient, MessageHandler messageHandler)
        {
            this.botClient = botClient;
            this.messageHandler = messageHandler;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            await messageHandler.HandleUpdateAsync(botClient, update, CancellationToken.None);
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

            disposed = true;
        }
    }
}
