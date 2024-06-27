using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UVindexTGBot
{
    internal class BotManager
    {
        internal string BotToken { get; set; }
        public float Uvi { get; set; }
        public double LatitudeFromUser { get; set; }
        public double LongitudeFromUser { get; set; }
        private ApiManager Api { get; set; }
        private bool IsLocationReceived = false;


        internal BotManager(string botToken, ApiManager api)
        {
            BotToken = botToken;
            Api = api;
        }

        public async Task StartReceiving()
        {
            var botClient = new TelegramBotClient(BotToken);
            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine($"Bot has started");
            Console.ReadLine();

            await cts.CancelAsync();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id;

            if (message.Text is not null && message.Text.StartsWith("/start"))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "There are two ways to use this bot: " +
                    "\n\t\u2022 You can check the current UV index using the /getuv command. " +
                    "\n\t\u2022 You can also get updates on the UV index at specific intervals (like every hour) using the /setintervals command. " +
                    "You'll receive messages from sunrise to sunset because the UV index is 0 at night.",
                    cancellationToken: cancellationToken);
            }

            if (message.Text is not null && (message.Text.StartsWith("/getuv") || message.Text.StartsWith("/changelocation")) && !IsLocationReceived)
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
                    replyMarkup: replyKeyboard
                );
            }
            else if (message.Type == MessageType.Location && !IsLocationReceived)
            {
                var location = message.Location;

                if (location == null) return;

                IsLocationReceived = true;
                LatitudeFromUser = location.Latitude;
                LongitudeFromUser = location.Longitude;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Location received.",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );

                Api.lat = LatitudeFromUser;
                Api.lon = LongitudeFromUser;
            }

            if (IsLocationReceived)
            {
                await Api.GetUvFromApi();
                Uvi = Api.Uvi;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Current UV index is {Math.Ceiling(Uvi)}",
                    cancellationToken: cancellationToken);
            }
        }



        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Polling error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
