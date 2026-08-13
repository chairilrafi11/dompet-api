using System.Security.Claims;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Dompet.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dompet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;
    public CategoriesController(ICategoryService categories) => _categories = categories;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> Get([FromQuery] CategoryType? type) =>
        Ok(await _categories.GetCategoriesAsync(UserId, type));

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryRequest request)
    {
        var category = await _categories.CreateCategoryAsync(UserId, request);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await _categories.UpdateCategoryAsync(UserId, id, request);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (deleted, error) = await _categories.DeleteCategoryAsync(UserId, id);
        if (error is not null) return BadRequest(new { error });
        return deleted ? NoContent() : NotFound();
    }
}
