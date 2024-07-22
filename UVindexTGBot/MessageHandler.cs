using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

namespace UVindexTGBot
{
    internal class MessageHandler
    {
        private readonly TelegramBotClient botClient;
        private readonly CancellationToken cancellationToken;
        private readonly LocationService locationService;
        private readonly UvUpdateScheduler uvUpdateScheduler;
        private long chatId;

        internal MessageHandler(TelegramBotClient botClient, LocationService locationService, UvUpdateScheduler uvUpdateScheduler, CancellationToken cancellationToken)
        {
            this.botClient = botClient;
            this.cancellationToken = cancellationToken;
            this.locationService = locationService;
            this.uvUpdateScheduler = uvUpdateScheduler;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type is UpdateType.CallbackQuery && update.CallbackQuery is not null)
                {
                    await HandleCallbackQueryAsync(update.CallbackQuery);
                }

                if (update.Message is not { } message)
                    return;

                chatId = message.Chat.Id;
                uvUpdateScheduler.ChatId = chatId;

                if (update.Type is UpdateType.Message && message.Text is not null)
                {
                    switch (message.Text.Split(' ')[0])
                    {
                        case "/start":
                            await HandleStartCommandAsync();
                            break;
                        case "/getuv":
                                await uvUpdateScheduler.SendUvUpdateAsync();
                            break;
                        case "/changelocation":
                            await locationService.RequestLocationAsync(chatId);
                            break;
                        case "/setintervals":
                            await HandleSetIntervalsCommandAsync();
                            break;
                        case "/cancelintervals":
                            uvUpdateScheduler.CancelUvUpdates();
                            break;
                    }
                }

                if (message.Type is MessageType.Location && !locationService.IsLocationReceived)
                {
                    await locationService.HandleLocationReceivedAsync(message);
                }
            }
            catch (ApiRequestException apiEx) when (apiEx.ErrorCode is 400 && apiEx.Message.Contains("query is too old"))
            {
                Console.WriteLine($"API request error: {apiEx.Message}");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in HandleUpdateAsync: {ex.Message}");
            }
        }

        private async Task HandleStartCommandAsync()
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "There are two ways to use this bot: " +
                      "\n\t\u2022 You can check the current UV index using the /getuv command. " +
                      "\n\t\u2022 You can also get updates on the UV index at specific intervals (like every hour) using the /setintervals command. " +
                      "You'll receive messages from sunrise to sunset because the UV index is 0 at night.",
                cancellationToken: cancellationToken);

            await locationService.RequestLocationAsync(chatId);
        }

        private async Task HandleSetIntervalsCommandAsync()
        {
            var buttons = new InlineKeyboardButton[][]
            {
                [
                    InlineKeyboardButton.WithCallbackData("1 hour", "1"),
                    InlineKeyboardButton.WithCallbackData("2 hours", "2"),
                    InlineKeyboardButton.WithCallbackData("Once per day", "24"),
                ],
            };

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please select an interval:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            byte one = 1;
            byte two = 2;
            byte twentyFour = 24;

            if (callbackQuery.Data == $"{one}")
            {
                await HandleIntervalInputAsync($"{one}");
            }
            else if (callbackQuery.Data == $"{two}")
            {
                await HandleIntervalInputAsync($"{two}");
            }
            else if (callbackQuery.Data == $"{twentyFour}")
            {
                await HandleIntervalInputAsync($"{twentyFour}");
            }

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: CancellationToken.None
            );
        }

        private async Task HandleIntervalInputAsync(string text)
        {
            string trimmedText = text.Trim();

            if (int.TryParse(trimmedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours))
            {
                var interval = TimeSpan.FromHours(hours);
                uvUpdateScheduler.ScheduleUvUpdates(interval);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"UV index updates set to every {hours} hours.",
                    cancellationToken: cancellationToken
                );

                await uvUpdateScheduler.SendUvUpdateAsync();
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
    }
}

