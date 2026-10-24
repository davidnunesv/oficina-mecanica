
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/pecas-insumos")]
public sealed class PecasInsumosController(PecaInsumoApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}", Name = "GetPecaInsumoById")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var peca = await service.GetByIdAsync(id, ct);

        return peca is null ? NotFound() : Ok(peca);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] PecaInsumoRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(request.Nome, request.Preco, ct);

        return CreatedAtRoute("GetPecaInsumoById", new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] PecaInsumoRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request.Nome, request.Preco, ct);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }
}
