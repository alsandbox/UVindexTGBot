using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace UVindexTGBot
{
    internal class LocationService(TelegramBotClient botClient, ApiManager api, CancellationToken cancellationToken)
    {
        public bool IsLocationReceived { get; set; }

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

            if (location is null || (location.Latitude <= 0 && location.Longitude <= 0))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Invalid location received. Please try again.",
                    cancellationToken: cancellationToken
                );

                await RequestLocationAsync(message.Chat.Id);
                return;
            }

            IsLocationReceived = true;
            double latitudeFromUser = location.Latitude;
            double longitudeFromUser = location.Longitude;

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Location received.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );

            api.Latitude = latitudeFromUser;
            api.Longitude = longitudeFromUser;
        }
    }
}

