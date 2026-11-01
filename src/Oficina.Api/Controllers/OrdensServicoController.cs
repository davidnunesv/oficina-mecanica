
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts;
using Oficina.Application.Services;
using Oficina.Application.UseCases.OrdensServico;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/ordens-servico")]
public sealed class OrdensServicoController(
    OrdemServicoApplicationService service,
    AprovarOrcamentoUseCase aprovarOrcamento,
    FinalizarOrdemServicoUseCase finalizarOrdemServico) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpGet("{id:guid}", Name = "GetOrdemServicoById")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var os = await service.GetByIdAsync(id, ct);

        return os is null ? NotFound() : Ok(os);
    }

    [HttpGet("{id:guid}/detalhes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetDetailsByIdAsync(Guid id, CancellationToken ct)
    {
        var os = await service.GetDetailsByIdAsync(id, ct);

        return os is null ? NotFound() : Ok(os);
    }

    [HttpGet("relatorios/tempo-medio")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTempoMedioExecucaoAsync(CancellationToken ct)
    {
        return Ok(await service.GetTempoMedioExecucaoAsync(ct));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateAsync(
    [FromBody] CriarOrdemServicoRequest request,
    CancellationToken ct)
    {
        var id = await service.CreateAsync(request.ClienteId, request.VeiculoId, ct);

        return CreatedAtRoute("GetOrdemServicoById", new { id }, new { id });
    }

    [HttpPost("{id:guid}/diagnostico/iniciar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> StartDiagnosisAsync(Guid id, CancellationToken ct)
    {
        var succeeded = await service.StartDiagnosisAsync(id, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/diagnostico")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddDiagnosisAsync(Guid id, [FromBody] RegistrarDiagnosticoRequest request, CancellationToken ct)
    {
        var succeeded = await service.AddDiagnosisAsync(id, request.Descricao, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/servicos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddServiceAsync(Guid id, [FromBody] AdicionarServicoRequest request, CancellationToken ct)
    {
        var succeeded = await service.AddServiceAsync(id, request.ServicoId, request.Quantidade, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/pecas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddPecaAsync(Guid id, [FromBody] AdicionarPecaRequest request, CancellationToken ct)
    {
        var succeeded = await service.AddPecaAsync(id, request.PecaId, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/orcamento")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GenerateOrcamentoAsync(Guid id, CancellationToken ct)
    {
        var succeeded = await service.GenerateOrcamentoAsync(id, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/orcamento/aprovar")]
    [Authorize(Policy = "ClienteOnly")]
    public async Task<IActionResult> AprovarOrcamentoAsync( Guid id, CancellationToken ct)
    {
        var claim = User.FindFirst("cliente_id")?.Value;

        if (!Guid.TryParse(claim, out var clienteId)
            || clienteId == Guid.Empty)
            return Forbid();

        var succeeded = await aprovarOrcamento.ExecuteAsync(id, clienteId, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/orcamento/rejeitar")]
    [Authorize(Policy = "ClienteOnly")]
    public async Task<IActionResult> RejeitarOrcamentoAsync(Guid id, CancellationToken ct)
    {
        var claim = User.FindFirst("cliente_id")?.Value;

        if (!Guid.TryParse(claim, out var clienteId)
            || clienteId == Guid.Empty)
            return Forbid();

        var succeeded = await service.RejeitarOrcamentoAsync(id, clienteId, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/finalizar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> FinalizarAsync(Guid id, CancellationToken ct)
    {
        var succeeded = await finalizarOrdemServico.ExecuteAsync(id, ct);

        return succeeded ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/entregar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EntregarAsync(Guid id, CancellationToken ct)
    {
        var succeeded = await service.Entregar(id, ct);

        return succeeded ? NoContent() : NotFound();
    }
}
