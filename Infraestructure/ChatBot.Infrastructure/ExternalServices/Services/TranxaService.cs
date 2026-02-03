using ChatBot.Application.DTOs.Tranza;
using ChatBot.Application.Interfaces.External;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatBot.Infrastructure.ExternalServices.Tranxa;

public class TranxaService : ITranxaService
{
    private readonly HttpClient _httpClient;
    private readonly ITranxaTokenService _tokenService;

    public TranxaService(HttpClient httpClient, ITranxaTokenService tokenService)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Valida identidad y retorna productos, saldo y movimientos desde UltraRed
    /// </summary>
    public async Task<UltraRedResponseDto?> GetCustomerProductsAsync(string documentNumber, string docType)
    {
        var request = await CreateUltraRedRequestAsync(new
        {
            idNumber = documentNumber,
            docType,
            idClient = (string?)null
        });

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"UltraRed ultraredCustTrans response: {content}");

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<UltraRedResponseDto>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// Bloquea tarjeta
    /// </summary>
    public async Task<BlockCardResponseDto?> BlockCardAsync(string tokenId, int codeBlock)
    {
        var token = await _tokenService.GetTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new BlockCardRequestDto
        {
            TokenId = tokenId,
            CodeBlock = codeBlock
        };

        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/Tarjetas/blockCard", json);

        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<BlockCardResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Crea request autenticado hacia UltraRed
    /// </summary>
    private async Task<HttpRequestMessage> CreateUltraRedRequestAsync(object body)
    {
        var token = await _tokenService.GetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Tarjetas/ultraredCustTrans");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var json = JsonSerializer.Serialize(body);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return request;
    }
}
