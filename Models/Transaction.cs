namespace Dompet.Api.Models;

public enum TransactionType { Income, Expense }

public class Transaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int WalletId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
