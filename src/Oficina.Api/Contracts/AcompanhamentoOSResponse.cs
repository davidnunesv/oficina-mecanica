
namespace Oficina.Api.Contracts;

public sealed record AcompanhamentoOrdemServicoResponse(
    Guid OrdemServicoId,
    string Status);
