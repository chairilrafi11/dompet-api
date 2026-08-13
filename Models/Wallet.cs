namespace Dompet.Api.Models;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
