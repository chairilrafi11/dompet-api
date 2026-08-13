using Dompet.Api.Data;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dompet.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    public CategoryService(AppDbContext db) => _db = db;

    public async Task<List<CategoryDto>> GetCategoriesAsync(string userId, CategoryType? type)
    {
        var query = _db.Categories.AsNoTracking().Where(c => c.UserId == userId);
        if (type.HasValue) query = query.Where(c => c.Type == type.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type))
            .ToListAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(string userId, CategoryRequest request)
    {
        var category = new Category
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return new CategoryDto(category.Id, category.Name, category.Type);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(string userId, int id, CategoryRequest request)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null) return null;

        category.Name = request.Name.Trim();
        category.Type = request.Type;
        await _db.SaveChangesAsync();
        return new CategoryDto(category.Id, category.Name, category.Type);
    }

    public async Task<(bool Deleted, string? Error)> DeleteCategoryAsync(string userId, int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null) return (false, null);

        var inUse = await _db.Transactions.AnyAsync(t => t.CategoryId == id && t.UserId == userId);
        if (inUse) return (false, "Category is in use by transactions");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
