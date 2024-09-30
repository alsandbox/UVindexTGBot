using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace UVindexTGBot
{
    [ApiController]
    [Route("api/telegramwebhookcontroller")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly BotManager _botManager;
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(BotManager botManager, ILogger<TelegramWebhookController> logger)
        {
            _botManager = botManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            _logger.LogInformation("Received update: {Update}", JsonSerializer.Serialize(update));


            if (update == null)
            {
                _logger.LogWarning("Received null update.");
                return BadRequest("Invalid update payload.");
            }

            await _botManager.HandleUpdateAsync(update);
            return Ok();
        }
    }
}