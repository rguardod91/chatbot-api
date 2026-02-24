using ChatBot.Application.DTOs.Tranza;

namespace ChatBot.Application.Interfaces.External
{
    public interface ITranxaService
    {
        Task<OtpGenerationResponseDto?> GenerateOtpAsync(string username);
        Task<OtpValidationResponseDto?> ValidateOtpAsync(string username, string userOtp);

        Task<UltraRedResponseDto?> GetCustomerProductsAsync(string documentNumber, string docType);
        Task<BlockCardResponseDto?> BlockCardAsync(string tokenId, int codeBlock);
    }
}
