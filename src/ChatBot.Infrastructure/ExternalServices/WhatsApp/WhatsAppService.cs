using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatBot.Infrastructure.ExternalServices.WhatsApp
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public WhatsAppService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task SendTextMessageAsync(string phoneNumberId, string to, string message)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("[WHATSAPP] Iniciando envío de mensaje");
            Console.WriteLine($"[WHATSAPP] Destinatario: {to}");
            Console.WriteLine($"[WHATSAPP] PhoneNumberId: {phoneNumberId}");
            Console.WriteLine($"[WHATSAPP] Mensaje: {message}");
            Console.WriteLine($"[WHATSAPP] Timestamp: {DateTime.UtcNow}");

            try
            {
                var token = _config["WhatsApp:AccessToken"];

                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("[WHATSAPP] ERROR: El AccessToken no está configurado.");
                    throw new Exception("AccessToken de WhatsApp no configurado.");
                }

                var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";

                Console.WriteLine($"[WHATSAPP] URL destino: {url}");

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var body = new
                {
                    messaging_product = "whatsapp",
                    to = to,
                    text = new { body = message }
                };

                var jsonBody = JsonSerializer.Serialize(body);

                Console.WriteLine($"[WHATSAPP] Payload enviado: {jsonBody}");

                request.Content = new StringContent(
                    jsonBody,
                    Encoding.UTF8,
                    "application/json");

                Console.WriteLine("[WHATSAPP] Enviando solicitud HTTP a Meta Graph API...");

                var start = DateTime.UtcNow;

                var response = await _httpClient.SendAsync(request);

                var duration = DateTime.UtcNow - start;

                Console.WriteLine($"[WHATSAPP] Tiempo de respuesta: {duration.TotalMilliseconds} ms");

                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[WHATSAPP] Código HTTP: {(int)response.StatusCode}");
                Console.WriteLine($"[WHATSAPP] Respuesta API: {responseBody}");

                response.EnsureSuccessStatusCode();

                Console.WriteLine("[WHATSAPP] Mensaje enviado correctamente.");
                Console.WriteLine("===============================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("===============================================");
                Console.WriteLine("[WHATSAPP] ERROR al enviar mensaje");
                Console.WriteLine($"[WHATSAPP] Mensaje de error: {ex.Message}");
                Console.WriteLine($"[WHATSAPP] StackTrace: {ex.StackTrace}");
                Console.WriteLine("===============================================");

                throw;
            }
        }
    }
}