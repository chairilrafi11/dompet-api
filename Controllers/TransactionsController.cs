using System.Security.Claims;
using Dompet.Api.DTOs;
using Dompet.Api.Models;
using Dompet.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dompet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactions;
    public TransactionsController(ITransactionService transactions) => _transactions = transactions;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> Get(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int? categoryId,
        [FromQuery] int? walletId,
        [FromQuery] TransactionType? type)
    {
        return Ok(await _transactions.GetTransactionsAsync(UserId, dateFrom, dateTo, categoryId, walletId, type));
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] TransactionRequest request)
    {
        var (data, error) = await _transactions.CreateTransactionAsync(UserId, request);
        if (error is not null) return BadRequest(new { error });
        return CreatedAtAction(nameof(Get), new { id = data!.Id }, data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionDto>> Update(int id, [FromBody] TransactionRequest request)
    {
        var data = await _transactions.UpdateTransactionAsync(UserId, id, request);
        return data is null ? NotFound() : Ok(data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await _transactions.DeleteTransactionAsync(UserId, id) ? NoContent() : NotFound();
}
