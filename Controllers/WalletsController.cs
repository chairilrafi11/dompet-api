using System.Security.Claims;
using Dompet.Api.DTOs;
using Dompet.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dompet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _wallets;
    public WalletsController(IWalletService wallets) => _wallets = wallets;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<List<WalletDto>>> Get() =>
        Ok(await _wallets.GetWalletsAsync(UserId));

    [HttpPost]
    public async Task<ActionResult<WalletDto>> Create([FromBody] WalletRequest request)
    {
        var wallet = await _wallets.CreateWalletAsync(UserId, request);
        return CreatedAtAction(nameof(Get), new { id = wallet.Id }, wallet);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WalletDto>> Update(int id, [FromBody] WalletRequest request)
    {
        var wallet = await _wallets.UpdateWalletAsync(UserId, id, request);
        return wallet is null ? NotFound() : Ok(wallet);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await _wallets.DeleteWalletAsync(UserId, id) ? NoContent() : NotFound();
}
