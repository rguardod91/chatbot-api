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

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        Console.WriteLine("===== WHATSAPP WEBHOOK RECIBIDO =====");
        Console.WriteLine($"Timestamp: {DateTime.UtcNow}");

        try
        {
            var entry = payload.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (!value.TryGetProperty("messages", out var messages))
            {
                Console.WriteLine("No messages property found.");
                return Ok();
            }

            var message = messages[0];

            if (!message.TryGetProperty("text", out var textElement))
            {
                Console.WriteLine("Message does not contain text.");
                return Ok();
            }

            var from = message.GetProperty("from").GetString()!;
            var text = textElement.GetProperty("body").GetString()!;

            Console.WriteLine($"Incoming message from: {from}");
            Console.WriteLine($"Message text: {text}");

            var phoneNumberId = value
                .GetProperty("metadata")
                .GetProperty("phone_number_id")
                .GetString()!;

            Console.WriteLine($"PhoneNumberId detected: {phoneNumberId}");

            Console.WriteLine("Calling BotConversationEngine...");

            var responses = await _engine.ProcessMessageAsync(from, text);

            Console.WriteLine($"Bot responses count: {responses.Count}");

            foreach (var response in responses)
            {
                Console.WriteLine($"Sending message: {response}");

                await _whatsApp.SendTextMessageAsync(phoneNumberId, from, response);

                // pequeño delay para mejorar experiencia visual en WhatsApp
                await Task.Delay(500);
            }

            Console.WriteLine("All messages sent successfully.");

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("===== WEBHOOK ERROR =====");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);

            return Ok();
        }
    }

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        Console.WriteLine("Webhook verification request received.");

        var verifyToken = _config["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == verifyToken)
        {
            Console.WriteLine("Webhook verified successfully.");
            return Ok(challenge);
        }

        Console.WriteLine("Webhook verification failed.");
        return Unauthorized();
    }
}