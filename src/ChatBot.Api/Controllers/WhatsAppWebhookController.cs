using ChatBot.Application.DTOs;
using ChatBot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ChatBot.Api.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly IBotConversationEngine _engine;

        public WhatsAppWebhookController(IBotConversationEngine engine)
        {
            _engine = engine;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] IncomingMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.MessageText))
                return BadRequest("Invalid message payload");

            var reply = await _engine.ProcessMessageAsync(dto.PhoneNumber, dto.MessageText);

            return Ok(new { response = reply });
        }
    }
}
