
namespace Oficina.Application.DTOs.Veiculos;

public sealed record VeiculoDto(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
