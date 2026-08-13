using Dompet.Api.DTOs;
using Dompet.Api.Models;

namespace Dompet.Api.Services;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetTransactionsAsync(
        string userId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int? categoryId, int? walletId, TransactionType? type);

    Task<(TransactionDto? Data, string? Error)> CreateTransactionAsync(string userId, TransactionRequest request);
    Task<TransactionDto?> UpdateTransactionAsync(string userId, int id, TransactionRequest request);
    Task<bool> DeleteTransactionAsync(string userId, int id);
}
