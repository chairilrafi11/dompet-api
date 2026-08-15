using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;
    public WalletService(AppDbContext db) => _db = db;

    public async Task<List<WalletDto>> GetWalletsAsync(string userId)
    {
        var wallets = await _db.Wallets.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Name)
            .Select(w => new { w.Id, w.Name, w.InitialBalance })
            .ToListAsync();

        var transactions = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.WalletId, t.Type, t.Amount })
            .ToListAsync();

        var net = transactions
            .GroupBy(t => t.WalletId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount));

        return wallets.Select(w =>
            new WalletDto(w.Id, w.Name, w.InitialBalance,
                w.InitialBalance + (net.TryGetValue(w.Id, out var n) ? n : 0)))
            .ToList();
    }

    public async Task<WalletDto> CreateWalletAsync(string userId, WalletRequest request)
    {
        var wallet = new Wallet
        {
            UserId = userId,
            Name = request.Name.Trim(),
            InitialBalance = request.InitialBalance,
        };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();
        return new WalletDto(wallet.Id, wallet.Name, wallet.InitialBalance, wallet.InitialBalance);
    }

    public async Task<WalletDto?> UpdateWalletAsync(string userId, int id, WalletRequest request)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (wallet is null) return null;

        wallet.Name = request.Name.Trim();
        wallet.InitialBalance = request.InitialBalance;
        await _db.SaveChangesAsync();

        var amounts = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.WalletId == id)
            .Select(t => t.Amount * (t.Type == TransactionType.Income ? 1 : -1))
            .ToListAsync();
        var net = amounts.Sum();

        return new WalletDto(wallet.Id, wallet.Name, wallet.InitialBalance, wallet.InitialBalance + net);
    }

    public async Task<bool> DeleteWalletAsync(string userId, int id)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (wallet is null) return false;

        _db.Wallets.Remove(wallet);
        await _db.SaveChangesAsync();
        return true;
    }
}
