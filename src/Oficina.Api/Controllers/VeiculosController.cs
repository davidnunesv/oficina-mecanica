
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/veiculos")]
public sealed class VeiculosController(VeiculoApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}", Name = "GetVeiculoById")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var veiculo = await service.GetByIdAsync(id, ct);

        return veiculo is null ? NotFound() : Ok(veiculo);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CriarVeiculoRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(
            request.ClienteId, request.Placa, request.Marca,
            request.Modelo, request.Ano, ct);

        return CreatedAtRoute("GetVeiculoById", new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] AtualizarVeiculoRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(
            id, request.Placa, request.Marca,
            request.Modelo, request.Ano, ct);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }
}
