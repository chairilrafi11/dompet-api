using Dompet.Api.DTOs;
using Dompet.Api.Models;

namespace Dompet.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(string userId, CategoryType? type);
    Task<CategoryDto> CreateCategoryAsync(string userId, CategoryRequest request);
    Task<CategoryDto?> UpdateCategoryAsync(string userId, int id, CategoryRequest request);
    Task<(bool Deleted, string? Error)> DeleteCategoryAsync(string userId, int id);
}
