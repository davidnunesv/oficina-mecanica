
namespace Oficina.Application.DTOs.OrdensServico;

public sealed record DiagnosticoDetalheDto(string Descricao, DateTimeOffset DataRegistro);

public sealed record ItemServicoDetalheDto(Guid ServicoId, int Quantidade, decimal PrecoAplicado, decimal Subtotal);

public sealed record ItemPecaDetalheDto(Guid PecaInsumoId, decimal PrecoAplicado);

public sealed record OrcamentoDetalheDto(Guid Id, decimal ValorServicos, decimal ValorPecas, decimal ValorTotal, string Status);

public sealed record OrdemServicoDetalheDto(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    DiagnosticoDetalheDto? Diagnostico,
    IReadOnlyList<ItemServicoDetalheDto> Servicos,
    IReadOnlyList<ItemPecaDetalheDto> Pecas,
    OrcamentoDetalheDto? Orcamento,
    DateTimeOffset? DataInicioExecucao,
    DateTimeOffset? DataFinalizacao);
