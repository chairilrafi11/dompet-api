using Dompet.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!db.Database.IsSqlServer()) return;
        if (await users.FindByEmailAsync("demo@dompet.app") is not null) return;

        var user = new ApplicationUser
        {
            UserName = "demo@dompet.app",
            Email = "demo@dompet.app",
            DisplayName = "Demo User",
        };

        if (!(await users.CreateAsync(user, "Demo123!")).Succeeded) return;

        var wallets = new List<Wallet>
        {
            new() { UserId = user.Id, Name = "Cash", InitialBalance = 500000m },
            new() { UserId = user.Id, Name = "Bank BCA", InitialBalance = 2000000m },
            new() { UserId = user.Id, Name = "GoPay", InitialBalance = 150000m },
        };
        db.Wallets.AddRange(wallets);

        var categories = new List<Category>
        {
            new() { UserId = user.Id, Name = "Gaji", Type = CategoryType.Income },
            new() { UserId = user.Id, Name = "Bonus", Type = CategoryType.Income },
            new() { UserId = user.Id, Name = "Makan", Type = CategoryType.Expense },
            new() { UserId = user.Id, Name = "Transport", Type = CategoryType.Expense },
            new() { UserId = user.Id, Name = "Belanja", Type = CategoryType.Expense },
            new() { UserId = user.Id, Name = "Tagihan", Type = CategoryType.Expense },
            new() { UserId = user.Id, Name = "Hiburan", Type = CategoryType.Expense },
            new() { UserId = user.Id, Name = "Lainnya", Type = CategoryType.Expense },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var incomeCategories = categories.Where(c => c.Type == CategoryType.Income).ToList();
        var expenseCategories = categories.Where(c => c.Type == CategoryType.Expense).ToList();
        var notes = new[] { "Makan siang", "Gojek", "Belanja bulanan", "Listrik", "Internet", "Nonton", "Bensin", "Lunch meeting", "Kopi", "Belanja dapur" };

        var rng = new Random(42);
        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>();

        for (var m = 0; m < 6; m++)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-m);

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                WalletId = wallets[0].Id,
                CategoryId = incomeCategories[0].Id,
                Amount = 5000000m,
                Type = TransactionType.Income,
                Note = "Gaji bulanan",
                Date = monthStart.AddDays(1),
            });

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                WalletId = wallets[1].Id,
                CategoryId = incomeCategories[1].Id,
                Amount = rng.Next(500, 1500) * 1000m,
                Type = TransactionType.Income,
                Note = "Bonus",
                Date = monthStart.AddDays(15),
            });

            for (var e = 0; e < 15; e++)
            {
                var category = expenseCategories[rng.Next(expenseCategories.Count)];
                var wallet = wallets[rng.Next(wallets.Count)];
                var amount = category.Name switch
                {
                    "Makan" => rng.Next(15, 150) * 1000m,
                    "Transport" => rng.Next(10, 80) * 1000m,
                    "Belanja" => rng.Next(100, 500) * 1000m,
                    "Tagihan" => rng.Next(100, 300) * 1000m,
                    "Hiburan" => rng.Next(50, 200) * 1000m,
                    _ => rng.Next(20, 150) * 1000m,
                };

                transactions.Add(new Transaction
                {
                    UserId = user.Id,
                    WalletId = wallet.Id,
                    CategoryId = category.Id,
                    Amount = amount,
                    Type = TransactionType.Expense,
                    Note = notes[rng.Next(notes.Length)],
                    Date = monthStart.AddDays(rng.Next(1, 28)).AddHours(rng.Next(8, 22)),
                });
            }
        }

        db.Transactions.AddRange(transactions);
        await db.SaveChangesAsync();
    }
}
