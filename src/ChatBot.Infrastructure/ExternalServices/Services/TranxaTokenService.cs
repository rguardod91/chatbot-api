using ChatBot.Application.Interfaces.External;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatBot.Infrastructure.ExternalServices.Services
{
    public class TranxaTokenService : ITranxaTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public TranxaTokenService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GetTokenAsync()
        {
            var form = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _config["Tranxa:ClientId"]! },
            { "client_secret", _config["Tranxa:ClientSecret"]! },
            { "scope", _config["Tranxa:Scope"]! }
        };

            var response = await _httpClient.PostAsync("/connect/token", new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
