
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/servicos")]
public sealed class ServicosController(ServicoApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}", Name = "GetServicoById")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var servico = await service.GetByIdAsync(id, ct);

        return servico is null ? NotFound() : Ok(servico);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ServicoRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(request.Nome, request.Descricao, request.Preco, ct);

        return CreatedAtRoute("GetServicoById", new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] ServicoRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(
            id, request.Nome, request.Descricao, request.Preco, ct);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }
}
