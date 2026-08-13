using Dompet.Api.DTOs;

namespace Dompet.Api.Services;

public interface IWalletService
{
    Task<List<WalletDto>> GetWalletsAsync(string userId);
    Task<WalletDto> CreateWalletAsync(string userId, WalletRequest request);
    Task<WalletDto?> UpdateWalletAsync(string userId, int id, WalletRequest request);
    Task<bool> DeleteWalletAsync(string userId, int id);
}
