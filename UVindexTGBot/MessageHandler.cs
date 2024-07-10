using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Globalization;

namespace UVindexTGBot
{
    internal class MessageHandler
    {
        private readonly TelegramBotClient botClient;
        private readonly CancellationToken cancellationToken;
        private readonly LocationService locationService;
        private readonly UvUpdateScheduler uvUpdateScheduler;
        private long chatId;
        private bool isWaitingForInterval;        

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
                if (update.Message is not { } message)
                    return;

                chatId = message.Chat.Id;
                uvUpdateScheduler.ChatId = chatId;

                if (message.Text is not null)
                {
                    switch (message.Text.Split(' ')[0])
                    {
                        case "/start":
                            await HandleStartCommandAsync();
                            break;
                        case "/getuv":
                            if (locationService.IsLocationReceived)
                                await uvUpdateScheduler.SendUvUpdateAsync();
                            else
                                await locationService.RequestLocationAsync(chatId);
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

                if (message.Type == MessageType.Location && !locationService.IsLocationReceived)
                {
                    await locationService.HandleLocationReceivedAsync(message);
                }

                if (isWaitingForInterval && !string.IsNullOrEmpty(message.Text))
                {
                    await HandleIntervalInputAsync(message.Text);
                }
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
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please provide the interval in hours (e.g., 1, 2, 3):",
                cancellationToken: cancellationToken
            );

            isWaitingForInterval = true;
        }

        private async Task HandleIntervalInputAsync(string text)
        {
            isWaitingForInterval = false;
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

