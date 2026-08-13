using Microsoft.AspNetCore.Identity;

namespace Dompet.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
