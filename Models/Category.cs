namespace Dompet.Api.Models;

public enum CategoryType { Income, Expense }

public class Category
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
}
