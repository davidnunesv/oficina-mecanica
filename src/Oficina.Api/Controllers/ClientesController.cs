
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(ClienteApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}", Name = "GetClienteById")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cliente = await service.GetByIdAsync(id, ct);

        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ClienteRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(request.Nome, request.Documento, ct);

        return CreatedAtRoute("GetClienteById", new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] ClienteRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request.Nome, request.Documento, ct);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }
}
