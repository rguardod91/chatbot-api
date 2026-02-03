using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
            var token = _config["WhatsApp:AccessToken"];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                messaging_product = "whatsapp",
                to = to,
                text = new { body = message }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"WhatsApp API response: {responseBody}");

            response.EnsureSuccessStatusCode();
        }
    }
}
