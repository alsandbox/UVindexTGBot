using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace UVindexTGBot
{
    public class LocationService
    {
        public bool IsLocationReceived { get; set; }
        private readonly ITelegramBotClient botClient;
        private readonly ApiManager api;

        public LocationService(ITelegramBotClient botClient, ApiManager api)
        {
            this.botClient = botClient;
            this.api = api;
        }

        public async Task RequestLocationAsync(long chatId, CancellationToken cancellationToken)
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
                text: "To receive the UV index, please share your location:",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
        }

        public async Task HandleLocationReceivedAsync(Message message, CancellationToken cancellationToken)
        {
            var location = message.Location;

            if (location is null || (location.Latitude <= 0 && location.Longitude <= 0))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Invalid location received. Please try again.",
                    cancellationToken: cancellationToken
                );

                await RequestLocationAsync(message.Chat.Id, cancellationToken);
                return;
            }

            IsLocationReceived = true;
            double latitudeFromUser = location.Latitude;
            double longitudeFromUser = location.Longitude;

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Location received. Now you can start getting a UV index. " +
                $"To do this, use the commands /getuv or /setintervals. ",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            api.Latitude = latitudeFromUser;
            api.Longitude = longitudeFromUser;
        }
    }
}

