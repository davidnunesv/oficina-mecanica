
namespace Oficina.Application.DTOs.OrdensServico;

public sealed record OrdemServicoDto(Guid Id, Guid ClienteId, Guid VeiculoId, string Status, string? StatusOrcamento);
