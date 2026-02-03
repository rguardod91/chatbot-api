using ChatBot.Application.DTOs.Tranza;

namespace ChatBot.Application.Interfaces.External
{
    public interface ITranxaService
    {
        Task<UltraRedResponseDto?> GetCustomerProductsAsync(string documentNumber, string docType);
        Task<BlockCardResponseDto?> BlockCardAsync(string tokenId, int codeBlock);

    }
}
