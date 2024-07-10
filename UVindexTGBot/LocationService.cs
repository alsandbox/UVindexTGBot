using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace UVindexTGBot
{
    internal class LocationService(TelegramBotClient botClient, ApiManager api, CancellationToken cancellationToken)
    {
        private bool isLocationReceived;
        public bool IsLocationReceived => isLocationReceived;

        public async Task RequestLocationAsync(long chatId)
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

        public async Task HandleLocationReceivedAsync(Message message)
        {
            var location = message.Location;

            if (location == null) return;

            isLocationReceived = true;
            double latitudeFromUser = location.Latitude;
            double longitudeFromUser = location.Longitude;

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Location received.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            api.Longitude = latitudeFromUser;
            api.Longitude = longitudeFromUser;
        }
    }
}

