using ChatBot.Application.Interfaces.Services;
using ChatBot.Infrastructure.ExternalServices.WhatsApp;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChatBot.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppMetaWebhookController : ControllerBase
{
    private readonly IBotConversationEngine _engine;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConfiguration _config;

    public WhatsAppMetaWebhookController(
        IBotConversationEngine engine,
        IWhatsAppService whatsApp,
        IConfiguration config)
    {
        _engine = engine;
        _whatsApp = whatsApp;
        _config = config;
    }

    // 📩 RECEPCIÓN DE MENSAJES DESDE META
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        try
        {
            var entry = payload.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (!value.TryGetProperty("messages", out var messages))
                return Ok();

            var message = messages[0];

            if (!message.TryGetProperty("text", out var textElement))
                return Ok();

            var from = message.GetProperty("from").GetString()!;
            var text = textElement.GetProperty("body").GetString()!;

            // 👇 NUEVO: obtener phone_number_id correcto
            var phoneNumberId = value.GetProperty("metadata")
                                     .GetProperty("phone_number_id")
                                     .GetString()!;

            var response = await _engine.ProcessMessageAsync(from, text);

            // 👇 CAMBIO: enviar usando el phoneNumberId recibido
            await _whatsApp.SendTextMessageAsync(phoneNumberId, from, response);

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook error: {ex.Message}");
            return Ok();
        }
    }


    // 🔐 VERIFICACIÓN INICIAL DEL WEBHOOK
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var verifyToken = _config["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == verifyToken)
            return Ok(challenge);

        return Unauthorized();
    }

}
