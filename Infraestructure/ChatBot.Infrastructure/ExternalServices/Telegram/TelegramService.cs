using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace ChatBot.Infrastructure.ExternalServices.Telegram
{
    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _http;
        private readonly string _token;

        public TelegramService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _token = config["Telegram:BotToken"]!;
        }

        public async Task SendMessageAsync(long chatId, string message)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "HTML"
            };

            var response = await _http.PostAsJsonAsync(url, payload);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Telegram API response: {body}");

            response.EnsureSuccessStatusCode();
        }
    }
}
