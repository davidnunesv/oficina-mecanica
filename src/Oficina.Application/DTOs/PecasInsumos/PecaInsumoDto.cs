
namespace Oficina.Application.DTOs.PecasInsumos;

public sealed record PecaInsumoDto(Guid Id, string Nome, decimal Preco, string Status, Guid? OrdemServicoId);
