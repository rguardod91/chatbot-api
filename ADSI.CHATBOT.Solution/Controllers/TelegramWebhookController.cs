using ChatBot.Application.Interfaces.Services;
using ChatBot.Infrastructure.ExternalServices.Telegram;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChatBot.Api.Controllers
{
    [ApiController]
    [Route("api/telegram/webhook")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly IBotConversationEngine _engine;
        private readonly ITelegramService _telegram;

        public TelegramWebhookController(
            IBotConversationEngine engine,
            ITelegramService telegram)
        {
            _engine = engine;
            _telegram = telegram;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] JsonElement update)
        {
            try
            {
                if (!update.TryGetProperty("message", out var message))
                    return Ok();

                var chatId = message.GetProperty("chat").GetProperty("id").GetInt64();
                var text = message.GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(text))
                    return Ok();

                var response = await _engine.ProcessMessageAsync(chatId.ToString(), text);

                await _telegram.SendMessageAsync(chatId, response);

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Telegram webhook error: {ex}");
                return Ok();
            }
        }
    }
}
