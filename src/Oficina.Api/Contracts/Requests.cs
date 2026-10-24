
using System.ComponentModel.DataAnnotations;

namespace Oficina.Api.Contracts;

public sealed class ClienteRequest
{
    [Required, MaxLength(150)]
    public string Nome { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Documento { get; init; } = string.Empty;
}

public sealed class CriarVeiculoRequest
{
    public Guid ClienteId { get; init; }

    [Required, MaxLength(8)]
    public string Placa { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Marca { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Modelo { get; init; } = string.Empty;

    [Range(1, 9999)]
    public int Ano { get; init; }
}

public sealed class AtualizarVeiculoRequest
{
    [Required, MaxLength(8)]
    public string Placa { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Marca { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Modelo { get; init; } = string.Empty;

    [Range(1, 9999)]
    public int Ano { get; init; }
}

public sealed class ServicoRequest
{
    [Required, MaxLength(150)]
    public string Nome { get; init; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Descricao { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Preco { get; init; }
}

public sealed class PecaInsumoRequest
{
    [Required, MaxLength(150)]
    public string Nome { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Preco { get; init; }
}

public sealed class CriarOrdemServicoRequest
{
    public Guid ClienteId { get; init; }
    public Guid VeiculoId { get; init; }
}

public sealed class RegistrarDiagnosticoRequest
{
    [Required, MaxLength(2000)]
    public string Descricao { get; init; } = string.Empty;
}

public sealed class AdicionarServicoRequest
{
    public Guid ServicoId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantidade { get; init; }
}

public sealed class AdicionarPecaRequest
{
    public Guid PecaId { get; init; }
}
