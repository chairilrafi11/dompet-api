using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    public TransactionService(AppDbContext db) => _db = db;

    public async Task<PageResult<TransactionDto>> GetTransactionsAsync(
        string userId, DateTime? dateFrom, DateTime? dateTo,
        int? categoryId, int? walletId, TransactionType? type,
        int page, int pageSize)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.UserId == userId);

        if (dateFrom.HasValue) query = query.Where(t => t.Date >= dateFrom.Value.ToUniversalTime());
        if (dateTo.HasValue) query = query.Where(t => t.Date <= dateTo.Value.ToUniversalTime());
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        if (walletId.HasValue) query = query.Where(t => t.WalletId == walletId.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
               t.Id, t.WalletId, t.Wallet.Name, t.CategoryId, t.Category.Name,
               t.Amount, t.Type, t.Note, t.Date))
           .ToListAsync();

        return new PageResult<TransactionDto>(
            items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize)
        );
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
            Date = request.Date?.ToUniversalTime() ?? DateTime.UtcNow,
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
        if (request.Date.HasValue) transaction.Date = request.Date.Value.ToUniversalTime();

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
