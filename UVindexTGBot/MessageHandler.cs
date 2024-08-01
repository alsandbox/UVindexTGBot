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
        private ApiManager api;
        private long chatId;
        private bool isAwaitingCustomIntervalInput;

        internal MessageHandler(TelegramBotClient botClient, ApiManager api, LocationService locationService, UvUpdateScheduler uvUpdateScheduler, CancellationToken cancellationToken)
        {
            this.botClient = botClient;
            this.api = api;
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
                    if (isAwaitingCustomIntervalInput)
                    {
                        await HandleIntervalInputAsync(message.Text);
                    }
                    else
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
                }

                if (message.Type is MessageType.Location)
                {
                    await locationService.HandleLocationReceivedAsync(message);

                    if (locationService.IsLocationReceived)
                    {
                        locationService.IsLocationReceived = false;
                    }
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
                    InlineKeyboardButton.WithCallbackData("3 hours", "3"),
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Once per day", "24"),
                    InlineKeyboardButton.WithCallbackData("Custom", "custom"),
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
            switch (callbackQuery.Data)
            {
                case "1":
                case "2":
                case "3":
                case "24":
                    await HandleIntervalInputAsync(callbackQuery.Data);
                    break;
                case "custom":
                    isAwaitingCustomIntervalInput = true;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Please enter a custom interval in hours (between 1 and 24):",
                        cancellationToken: cancellationToken
                    );
                    break;
            }

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: CancellationToken.None
            );
        }

        private async Task HandleIntervalInputAsync(string text)
        {
            string trimmedText = text.Trim();


            if (int.TryParse(trimmedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours) 
                && hours >= 1 && hours <= 24)
            {
                var interval = TimeSpan.FromHours(hours);
                uvUpdateScheduler.ScheduleUvUpdates(interval);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"UV index updates set to every {hours} hours.",
                    cancellationToken: cancellationToken
                );

                if (isAwaitingCustomIntervalInput)
                {
                    isAwaitingCustomIntervalInput = false;
                }
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

