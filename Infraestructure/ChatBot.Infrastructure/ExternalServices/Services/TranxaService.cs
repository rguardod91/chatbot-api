using ChatBot.Application.DTOs.Tranza;
using ChatBot.Application.Interfaces.External;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ChatBot.Infrastructure.ExternalServices.Services;

public class TranxaService : ITranxaService
{
    private readonly HttpClient _client;
    private readonly ITranxaTokenService _tokenService;

    public TranxaService(HttpClient client, ITranxaTokenService tokenService)
    {
        _client = client;
        _tokenService = tokenService;
    }

    private async Task PrepareAuthenticatedClientAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var token = await _tokenService.GetTokenAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // ---------- OTP GENERATION ----------
    public async Task<OtpGenerationResponseDto?> GenerateOtpAsync(string username)
    {
        await PrepareAuthenticatedClientAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/Clientes/sendotpgenerated",
            new { username });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OtpGenerationResponseDto>();
    }

    // ---------- OTP VALIDATION ----------
    public async Task<OtpValidationResponseDto?> ValidateOtpAsync(string username, string userOtp)
    {
        await PrepareAuthenticatedClientAsync();

        var body = new
        {
            username = username,
            userOtp = userOtp
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Clientes/validateotp",
            body);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OtpValidationResponseDto>();
    }


    // ---------- PRODUCTS ----------
    public async Task<UltraRedResponseDto?> GetCustomerProductsAsync(string documentNumber, string docType)
    {
        await PrepareAuthenticatedClientAsync();

        var body = new
        {
            idNumber = documentNumber,
            docType = docType,
            idClient = (string?)null
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Tarjetas/ultraredCustTrans",
            body);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"UltraRed error: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadFromJsonAsync<UltraRedResponseDto>();
    }


    // ---------- BLOCK CARD ----------
    public async Task<BlockCardResponseDto?> BlockCardAsync(string tokenId, int codeBlock)
    {
        await PrepareAuthenticatedClientAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/Tarjetas/blockcard",
            new { tokenId, codeBlock });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BlockCardResponseDto>();
    }
}
