
using Oficina.Application.DTOs.OrdensServico;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.Application.Services;

public class OrdemServicoApplicationService(
    IOrdemServicoRepository ordemServicoRepository,
    IClienteRepository clienteRepository,
    IVeiculoRepository veiculoRepository,
    IServicoRepository servicoRepository,
    IPecaInsumoRepository pecaInsumoRepository)
{
    private readonly IOrdemServicoRepository _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
    private readonly IClienteRepository _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
    private readonly IVeiculoRepository _veiculoRepository = veiculoRepository ?? throw new ArgumentNullException(nameof(veiculoRepository));
    private readonly IServicoRepository _servicoRepository = servicoRepository ?? throw new ArgumentNullException(nameof(servicoRepository));
    private readonly IPecaInsumoRepository _pecaInsumoRepository = pecaInsumoRepository ?? throw new ArgumentNullException(nameof(pecaInsumoRepository));


    public async Task<Guid> CreateAsync(Guid clienteId, Guid veiculoId, CancellationToken cancellationToken = default)
    {
        var ordemServico = new OrdemServico(clienteId, veiculoId);

        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);

        if (cliente is null)
            throw new KeyNotFoundException("O cliente informado não foi encontrado.");

        var veiculo = await _veiculoRepository.GetByIdAsync(veiculoId, cancellationToken);

        if (veiculo is null)
            throw new KeyNotFoundException("O veículo informado não foi encontrado.");

        if (veiculo.ClienteId != cliente.Id)
            throw new InvalidOperationException("O veículo não pertence ao cliente informado.");

        await _ordemServicoRepository.CreateAsync(ordemServico, cancellationToken);

        return ordemServico.Id;
    }

    public async Task<OrdemServicoDto?> GetByIdAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        return ordemServico is null ? null : MapToDto(ordemServico);
    }

    public async Task<IReadOnlyList<OrdemServicoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var ordensServico = await _ordemServicoRepository.ListAsync(cancellationToken);

        return ordensServico.Select(MapToDto).ToList();
    }


    public async Task<bool> StartDiagnosisAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        ordemServico.CreateDiagnostic();

        await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

        return true;
    }

    public async Task<bool> AddDiagnosisAsync(Guid ordemServicoId, string descricao, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        ordemServico.AddDiagnostic(descricao);

        await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

        return true;
    }


    public async Task<bool> AddServiceAsync(Guid ordemServicoId, Guid servicoId, int quantidade, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        if (servicoId == Guid.Empty)
            throw new ArgumentException("O ID do serviço é obrigatório.", nameof(servicoId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        var servico = await _servicoRepository.GetByIdAsync(servicoId, cancellationToken);

        if (servico is null)
            throw new KeyNotFoundException("O serviço informado não foi encontrado.");

        ordemServico.AdicionarServico(servico.Id, quantidade, servico.Preco);

        await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

        return true;
    }


    public async Task<bool> AddPecaAsync(Guid ordemServicoId, Guid pecaId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        if (pecaId == Guid.Empty)
            throw new ArgumentException("O ID da peça é obrigatório.", nameof(pecaId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        var peca = await _pecaInsumoRepository.GetByIdAsync(pecaId, cancellationToken);

        if (peca is null)
            throw new KeyNotFoundException("A peça informada não foi encontrada.");

        if (peca.Status != StatusPecaInsumo.Disponivel)
            throw new InvalidOperationException("A peça não está disponível para inclusão na Ordem de Serviço.");

        ordemServico.AdicionarPeca(peca.Id, peca.Preco);

        await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

        return true;
    }


    public async Task<bool> GenerateOrcamentoAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (ordemServico is null)
            return false;

        ordemServico.GerarOrcamento();

        await _ordemServicoRepository.UpdateAsync(ordemServico, cancellationToken);

        return true;
    }


    public async Task<bool> RejeitarOrcamentoAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (os is null)
            return false;

        os.RejeitarOrcamento();

        await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

        return true;
    }


    public async Task<bool> Entregar(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        if (os is null)
            return false;

        os.Entregar();

        await _ordemServicoRepository.UpdateAsync(os, cancellationToken);

        return true;
    }


    public async Task<OrdemServicoDetalheDto?> GetDetailsByIdAsync(Guid ordemServicoId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, cancellationToken);

        return os is null ? null : MapToDetailsDto(os);
    }


    public async Task<TempoMedioExecucaoDto> GetTempoMedioExecucaoAsync(CancellationToken cancellationToken = default)
    {
        var ordens = await _ordemServicoRepository.ListAsync(cancellationToken);

        var duracoes = ordens
            .Where(os => (os.Status is StatusOS.Finalizada or StatusOS.Entregue)
                && os.DataInicioExecucao.HasValue
                && os.DataFinalizacao.HasValue
                && os.DataFinalizacao.Value >= os.DataInicioExecucao.Value)
            .Select(os => os.DataFinalizacao!.Value - os.DataInicioExecucao!.Value)
            .ToList();

        if (duracoes.Count == 0)
            return new TempoMedioExecucaoDto(0, null);

        var mediaTicks = (long)duracoes.Average(duracao => duracao.Ticks);

        return new TempoMedioExecucaoDto(duracoes.Count, TimeSpan.FromTicks(mediaTicks));
    }



    private static OrdemServicoDto MapToDto(OrdemServico ordemServico)
    {
        return new OrdemServicoDto(
            ordemServico.Id,
            ordemServico.ClienteId,
            ordemServico.VeiculoId,
            ordemServico.Status.ToString(),
            ordemServico.Orcamento?.Status.ToString()
        );
    }

    private static OrdemServicoDetalheDto MapToDetailsDto(OrdemServico os)
    {
        var diagnostico = os.Diagnostico is null
            ? null
            : new DiagnosticoDetalheDto(os.Diagnostico.Descricao, os.Diagnostico.DataRegistro);

        var servicos = os.ItensServico
            .Select(item => new ItemServicoDetalheDto(item.ServicoId, item.Quantidade, item.PrecoAplicado, item.CalcularSubtotal()))
            .ToList();

        var pecas = os.ItensPeca
            .Select(item => new ItemPecaDetalheDto(item.PecaInsumoId, item.PrecoAplicado))
            .ToList();

        var orcamento = os.Orcamento is null
            ? null
            : new OrcamentoDetalheDto(os.Orcamento.Id, os.Orcamento.ValorServicos, os.Orcamento.ValorPecas, os.Orcamento.ValorTotal, os.Orcamento.Status.ToString());

        return new OrdemServicoDetalheDto(
            os.Id,
            os.ClienteId,
            os.VeiculoId,
            os.Status.ToString(),
            diagnostico,
            servicos,
            pecas,
            orcamento,
            os.DataInicioExecucao,
            os.DataFinalizacao);
    }


}
