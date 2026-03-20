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
        Console.WriteLine("===============================================");
        Console.WriteLine("[TRANXA] Preparando cliente autenticado");

        try
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var token = await _tokenService.GetTokenAsync();

            Console.WriteLine("[TRANXA] Token obtenido correctamente");

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TRANXA] ERROR obteniendo token");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    // ---------- OTP GENERATION ----------
    public async Task<OtpGenerationResponseDto?> GenerateOtpAsync(string username)
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("[TRANXA] Generando OTP");
        Console.WriteLine($"[TRANXA] Usuario: {username}");

        await PrepareAuthenticatedClientAsync();

        var start = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/Clientes/sendotpgenerated",
            new { username });

        var duration = DateTime.UtcNow - start;

        Console.WriteLine($"[TRANXA] Tiempo respuesta OTP: {duration.TotalMilliseconds} ms");
        Console.WriteLine($"[TRANXA] Código HTTP: {(int)response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TRANXA] Respuesta API: {body}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OtpGenerationResponseDto>();
    }

    // ---------- OTP VALIDATION ----------
    public async Task<OtpValidationResponseDto?> ValidateOtpAsync(string username, string userOtp)
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("[TRANXA] Validando OTP");
        Console.WriteLine($"[TRANXA] Usuario: {username}");

        await PrepareAuthenticatedClientAsync();

        var body = new
        {
            username = username,
            userOtp = userOtp
        };

        Console.WriteLine($"[TRANXA] Payload enviado: {System.Text.Json.JsonSerializer.Serialize(body)}");

        var start = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/Clientes/validateotp",
            body);

        var duration = DateTime.UtcNow - start;

        Console.WriteLine($"[TRANXA] Tiempo respuesta validación OTP: {duration.TotalMilliseconds} ms");
        Console.WriteLine($"[TRANXA] Código HTTP: {(int)response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TRANXA] Respuesta API: {responseBody}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OtpValidationResponseDto>();
    }

    // ---------- PRODUCTS ----------
    public async Task<UltraRedResponseDto?> GetCustomerProductsAsync(string documentNumber, string docType)
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("[TRANXA] Consultando productos del cliente");
        Console.WriteLine($"[TRANXA] Documento: {documentNumber}");
        Console.WriteLine($"[TRANXA] Tipo documento: {docType}");

        await PrepareAuthenticatedClientAsync();

        var body = new
        {
            idNumber = documentNumber,
            docType = docType,
            idClient = (string?)null
        };

        Console.WriteLine($"[TRANXA] Payload enviado: {System.Text.Json.JsonSerializer.Serialize(body)}");

        var start = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/Tarjetas/ultraredCustTrans",
            body);

        var duration = DateTime.UtcNow - start;

        Console.WriteLine($"[TRANXA] Tiempo respuesta productos: {duration.TotalMilliseconds} ms");
        Console.WriteLine($"[TRANXA] Código HTTP: {(int)response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TRANXA] Respuesta API: {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("[TRANXA] ERROR en consulta de productos");

            throw new Exception($"UltraRed error: {response.StatusCode} - {responseBody}");
        }

        return await response.Content.ReadFromJsonAsync<UltraRedResponseDto>();
    }

    // ---------- BLOCK CARD ----------
    public async Task<BlockCardResponseDto?> BlockCardAsync(string tokenId, int codeBlock)
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("[TRANXA] Bloqueando tarjeta");
        Console.WriteLine($"[TRANXA] TokenId: {tokenId}");
        Console.WriteLine($"[TRANXA] Código bloqueo: {codeBlock}");

        await PrepareAuthenticatedClientAsync();

        var body = new { tokenId, codeBlock };

        Console.WriteLine($"[TRANXA] Payload enviado: {System.Text.Json.JsonSerializer.Serialize(body)}");

        var start = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/Tarjetas/blockcard",
            body);

        var duration = DateTime.UtcNow - start;

        Console.WriteLine($"[TRANXA] Tiempo respuesta bloqueo: {duration.TotalMilliseconds} ms");
        Console.WriteLine($"[TRANXA] Código HTTP: {(int)response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TRANXA] Respuesta API: {responseBody}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BlockCardResponseDto>();
    }
}