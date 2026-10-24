
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize(Policy = "ClienteOnly")]
[Route("api/acompanhamento")]
public sealed class AcompanhamentoController(
    OrdemServicoApplicationService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ConsultarAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // A identidade do cliente é obtida do JWT validado.
        var clienteIdClaim = User.FindFirst("cliente_id")?.Value;

        if (!Guid.TryParse(clienteIdClaim, out var clienteId) ||
            clienteId == Guid.Empty)
        {
            return Forbid();
        }

        var ordemServico = await service.GetByIdAsync(
            id,
            cancellationToken);

        if (ordemServico is null ||
            ordemServico.ClienteId != clienteId)
        {
            return NotFound();
        }

        var response = new AcompanhamentoOrdemServicoResponse(
            ordemServico.Id,
            ordemServico.Status);

        return Ok(response);
    }
}
