using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    public TransactionService(AppDbContext db) => _db = db;

    public async Task<List<TransactionDto>> GetTransactionsAsync(
        string userId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int? categoryId, int? walletId, TransactionType? type)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.UserId == userId);

        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        if (walletId.HasValue) query = query.Where(t => t.WalletId == walletId.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);

        var result = await query
            .Select(t => new TransactionDto(
                t.Id, t.WalletId, t.Wallet.Name, t.CategoryId, t.Category.Name,
                t.Amount, t.Type, t.Note, t.Date))
            .ToListAsync();

        var filtered = result.AsEnumerable();
        if (dateFrom.HasValue) filtered = filtered.Where(t => t.Date >= dateFrom.Value);
        if (dateTo.HasValue) filtered = filtered.Where(t => t.Date <= dateTo.Value);

        return filtered.OrderByDescending(t => t.Date).ToList();
    }

    public async Task<(TransactionDto?, string?)> CreateTransactionAsync(string userId, TransactionRequest request)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == userId);
        if (wallet is null) return (null, "Wallet not found");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
        if (category is null) return (null, "Category not found");

        if (category.Type != (CategoryType)request.Type)
            return (null, "Transaction type does not match category type");

        var transaction = new Transaction
        {
            UserId = userId,
            WalletId = request.WalletId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Type = request.Type,
            Note = request.Note,
            Date = request.Date,
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return (new TransactionDto(
            transaction.Id, transaction.WalletId, wallet.Name,
            transaction.CategoryId, category.Name, transaction.Amount,
            transaction.Type, transaction.Note, transaction.Date), null);
    }

    public async Task<TransactionDto?> UpdateTransactionAsync(string userId, int id, TransactionRequest request)
    {
        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction is null) return null;

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == userId);
        if (wallet is null) return null;

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
        if (category is null) return null;

        transaction.WalletId = request.WalletId;
        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Note = request.Note;
        transaction.Date = request.Date;

        await _db.SaveChangesAsync();

        return new TransactionDto(
            transaction.Id, transaction.WalletId, wallet.Name,
            transaction.CategoryId, category.Name, transaction.Amount,
            transaction.Type, transaction.Note, transaction.Date);
    }

    public async Task<bool> DeleteTransactionAsync(string userId, int id)
    {
        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction is null) return false;

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return true;
    }
}
