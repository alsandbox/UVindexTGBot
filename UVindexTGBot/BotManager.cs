using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UVindexTGBot
{
    internal class BotManager : IDisposable
    {
        internal string BotToken { get; set; }
        private long chatId;
        public float Uvi { get; set; }
        public double LatitudeFromUser { get; set; }
        public double LongitudeFromUser { get; set; }

        private ApiManager Api { get; set; }
        
        private readonly TelegramBotClient botClient;
        private System.Threading.Timer? timer;
        private readonly CancellationTokenSource cts;
        
        private TimeSpan interval;
        
        private bool IsWaitingForInterval = false;
        private bool IsLocationReceived = false;
        private bool disposed;


        internal BotManager(string botToken, ApiManager api)
        {
            this.botClient = new TelegramBotClient(botToken);
            Api = api;
            cts = new CancellationTokenSource();
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
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions
            );

            try
            {
                await Task.Delay(Timeout.Infinite);
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

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message)
                    return;

                chatId = message.Chat.Id;

                if (message.Text is not null && message.Text.StartsWith("/start"))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "There are two ways to use this bot: " +
                        "\n\t\u2022 You can check the current UV index using the /getuv command. " +
                        "\n\t\u2022 You can also get updates on the UV index at specific intervals (like every hour) using the /setintervals command. " +
                        "You'll receive messages from sunrise to sunset because the UV index is 0 at night.",
                        cancellationToken: cancellationToken);

                    await RequestLocationAsync(chatId, cancellationToken);
                }
                if(message.Text is not null && message.Text.StartsWith("/getuv") && IsLocationReceived)
                {
                    await SendUvUpdateAsync(chatId, cancellationToken);
                }
                if (message.Text is not null && message.Text.StartsWith("/changelocation") && !IsLocationReceived)
                {
                    await RequestLocationAsync(chatId, cancellationToken);
                }
                if (message.Type == MessageType.Location && !IsLocationReceived)
                {
                    await HandleLocationReceivedAsync(message, cancellationToken);
                }

                if (message.Text is not null && message.Text.StartsWith("/setintervals"))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please provide the interval in hours (e.g., 1, 2, 3):",
                        cancellationToken: cancellationToken
                    );

                    message.Text = null;
                    IsWaitingForInterval = true;
                }

                if (IsWaitingForInterval && !string.IsNullOrEmpty(message.Text))
                {
                    IsWaitingForInterval = false;
                    string trimmedText = message.Text.Trim();

                    if (int.TryParse(trimmedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours))
                    {
                        interval = TimeSpan.FromHours(hours);
                        ScheduleUvUpdates();

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"UV index updates set to every {hours} hours.",
                            cancellationToken: cancellationToken
                        );

                        await SendUvUpdateAsync(chatId, cancellationToken);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Invalid input. Please provide a valid number of hours.",
                            cancellationToken: cancellationToken
                        );
                    }
                }

                if (message.Text is not null && message.Text.StartsWith("/cancelintervals"))
                {
                    timer?.Change(Timeout.Infinite, Timeout.Infinite);

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "UV index updates have been cancelled.",
                        cancellationToken: cts.Token
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in HandleUpdateAsync: {ex.Message}");
            }
        }

        private async Task RequestLocationAsync(long chatId, CancellationToken cancellationToken)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("Send Location")
                {
                    RequestLocation = true
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please share your location:",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleLocationReceivedAsync(Message message, CancellationToken cancellationToken)
        {
            var location = message.Location;

            if (location == null) return;

            IsLocationReceived = true;
            LatitudeFromUser = location.Latitude;
            LongitudeFromUser = location.Longitude;

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Location received.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            Api.lat = LatitudeFromUser;
            Api.lon = LongitudeFromUser;
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Polling error: {exception.Message}");
            return Task.CompletedTask;
        }

        private void ScheduleUvUpdates()
        {
            timer = new Timer(async state => await SendUvUpdateAsync(chatId, cts.Token), null, TimeSpan.Zero, interval);
        }

        private async Task SendUvUpdateAsync(long chatId, CancellationToken cancellationToken)
        {
            await Api.GetUvFromApi();
                Uvi = Api.Uvi;

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Current UV index is {Uvi}",
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
                cts?.Dispose();
            }

            disposed = true;
        }
    }
}
