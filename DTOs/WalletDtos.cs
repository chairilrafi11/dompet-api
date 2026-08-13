using System.ComponentModel.DataAnnotations;

namespace Dompet.Api.DTOs;

public record WalletRequest([Required] string Name, decimal InitialBalance);

public record WalletDto(int Id, string Name, decimal InitialBalance, decimal Balance);
