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
            var (body, menu) = SplitMenu(message);

            // Mostrar loader solo si es operación financiera real
            if (ShouldShowLoader(body))
            {
                await SendRaw(chatId, "⏳ <b>Procesando transacción…</b>");
                await Task.Delay(500);
            }

            if (!string.IsNullOrWhiteSpace(body))
                await SendRaw(chatId, body);

            if (menu.Any())
                await SendMenu(chatId, menu);
        }
        private bool ShouldShowLoader(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.Contains("Saldo disponible") ||
                   message.Contains("Últimos movimientos") ||
                   message.Contains("Resultado bloqueo") ||
                   message.Contains("Tus productos disponibles");
        }

        private async Task SendRaw(long chatId, string text)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "HTML"
            };

            await _http.PostAsJsonAsync(url, payload);
        }


        private async Task SendMenu(long chatId, List<(string text, string value)> options)
        {
            var url = $"https://api.telegram.org/bot{_token}/sendMessage";

            var keyboard = new
            {
                inline_keyboard = options
                    .Select(o => new[]
                    {
                new { text = o.text, callback_data = o.value }
                    })
            };

            var payload = new
            {
                chat_id = chatId,
                text = "💳 <b>Tarjeta de Crédito</b>\nSelecciona una opción:",
                parse_mode = "HTML",
                reply_markup = keyboard
            };

            await _http.PostAsJsonAsync(url, payload);
        }


        // 🔎 Detecta si el mensaje contiene el menú numerado del Engine
        private (string body, List<(string text, string value)> menu) SplitMenu(string message)
        {
            if (!message.Contains("¿Qué deseas hacer ahora?"))
                return (message, new());

            var parts = message.Split("¿Qué deseas hacer ahora?");
            var body = parts[0].Trim();

            var options = new List<(string text, string value)>
    {
        ("💰 Consultar saldo", "1"),
        ("📊 Ver movimientos", "2"),
        ("🔒 Bloquear tarjeta", "3"),
        ("💼 Cambiar producto", "4"),
        ("🚪 Salir", "5")
    };

            return (body, options);
        }
    }
}
