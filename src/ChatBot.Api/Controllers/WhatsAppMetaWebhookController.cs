using ChatBot.Application.Interfaces.Services;
using ChatBot.Infrastructure.ExternalServices.WhatsApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ChatBot.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppMetaWebhookController : ControllerBase
{
    private readonly IBotConversationEngine _engine;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public WhatsAppMetaWebhookController(
        IBotConversationEngine engine,
        IWhatsAppService whatsApp,
        IConfiguration config,
        IMemoryCache cache)
    {
        _engine = engine;
        _whatsApp = whatsApp;
        _config = config;
        _cache = cache;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        try
        {
            Console.WriteLine("========== WHATSAPP WEBHOOK ==========");
            Console.WriteLine($"Timestamp: {DateTime.UtcNow}");

            if (!payload.TryGetProperty("entry", out var entryArray))
                return Ok();

            var entry = entryArray[0];

            if (!entry.TryGetProperty("changes", out var changesArray))
                return Ok();

            var changes = changesArray[0];

            if (!changes.TryGetProperty("value", out var value))
                return Ok();

            //---------------------------------------
            // Ignorar status
            //---------------------------------------

            if (value.TryGetProperty("statuses", out _))
            {
                Console.WriteLine("[WEBHOOK] Evento status ignorado");
                return Ok();
            }

            //---------------------------------------
            // Validar mensajes
            //---------------------------------------

            if (!value.TryGetProperty("messages", out var messages))
                return Ok();

            var message = messages[0];

            //---------------------------------------
            // OBTENER MESSAGE ID
            //---------------------------------------

            if (!message.TryGetProperty("id", out var idElement))
                return Ok();

            var messageId = idElement.GetString();

            if (string.IsNullOrWhiteSpace(messageId))
                return Ok();

            //---------------------------------------
            // DEDUPLICACIÓN
            //---------------------------------------

            if (_cache.TryGetValue(messageId, out _))
            {
                Console.WriteLine($"[WEBHOOK] Mensaje duplicado ignorado: {messageId}");
                return Ok();
            }

            _cache.Set(messageId, true, TimeSpan.FromMinutes(5));

            //---------------------------------------
            // Validar tipo
            //---------------------------------------

            if (!message.TryGetProperty("type", out var typeElement))
                return Ok();

            var type = typeElement.GetString();

            if (type != "text")
            {
                Console.WriteLine($"[WEBHOOK] Tipo mensaje ignorado: {type}");
                return Ok();
            }

            //---------------------------------------
            // Obtener texto
            //---------------------------------------

            var text = message.GetProperty("text")
                              .GetProperty("body")
                              .GetString();

            if (string.IsNullOrWhiteSpace(text))
                return Ok();

            //---------------------------------------
            // Obtener usuario
            //---------------------------------------

            var from = message.GetProperty("from").GetString();

            //---------------------------------------
            // Obtener phoneNumberId
            //---------------------------------------

            var phoneNumberId = value.GetProperty("metadata")
                                     .GetProperty("phone_number_id")
                                     .GetString();

            //---------------------------------------
            // Protección contra loop
            //---------------------------------------

            if (from == phoneNumberId)
            {
                Console.WriteLine("[WEBHOOK] Mensaje propio ignorado");
                return Ok();
            }

            Console.WriteLine($"[WEBHOOK] Mensaje recibido de {from}");
            Console.WriteLine($"[WEBHOOK] Texto: {text}");

            //---------------------------------------
            // Ejecutar motor
            //---------------------------------------

            var responses = await _engine.ProcessMessageAsync(from!, text!);

            if (responses == null || !responses.Any())
            {
                Console.WriteLine("[WEBHOOK] Engine no retornó respuestas");
                return Ok();
            }

            //---------------------------------------
            // Enviar respuestas
            //---------------------------------------

            foreach (var response in responses)
            {
                Console.WriteLine($"[WEBHOOK] Enviando: {response}");

                await _whatsApp.SendTextMessageAsync(
                    phoneNumberId!,
                    from!,
                    response);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== ERROR WEBHOOK ==========");
            Console.WriteLine(ex.ToString());

            return Ok();
        }
    }

    //---------------------------------------
    // VERIFY
    //---------------------------------------

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