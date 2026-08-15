using System.ComponentModel.DataAnnotations;
using Dompet.Api.Models;

namespace Dompet.Api.DTOs;

public record TransactionRequest(
    int WalletId,
    int CategoryId,
    [Range(0.01, double.MaxValue)] decimal Amount,
    TransactionType Type,
    string? Note,
    DateTime Date);

public record TransactionDto(
    int Id,
    int WalletId,
    string WalletName,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    TransactionType Type,
    string? Note,
    DateTimeOffset Date);
