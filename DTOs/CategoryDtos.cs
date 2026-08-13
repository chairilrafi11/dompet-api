using System.ComponentModel.DataAnnotations;
using Dompet.Api.Models;

namespace Dompet.Api.DTOs;

public record CategoryRequest([Required] string Name, CategoryType Type);

public record CategoryDto(int Id, string Name, CategoryType Type);
