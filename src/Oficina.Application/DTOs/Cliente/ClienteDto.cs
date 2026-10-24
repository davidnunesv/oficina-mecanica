
namespace Oficina.Application.DTOs.Clientes;

public sealed record ClienteDto(Guid Id, string Nome, string Documento, string TipoDocumento);
