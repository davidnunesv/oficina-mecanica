using Oficina.Domain.Enums;

namespace Oficina.Domain.Entities;

public class OrdemServico
{
    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOS Status { get; private set; }
    public DateTimeOffset? DataInicioExecucao { get; private set; }
    public DateTimeOffset? DataFinalizacao { get; private set; }
    private readonly List<ItemServico> _itensServico = new();
    private readonly List<ItemPecaOS> _itensPeca = new();
    public Orcamento? Orcamento { get; private set; }
    public Diagnostico? Diagnostico { get; private set; }


    public IReadOnlyList<ItemServico> ItensServico => _itensServico.AsReadOnly();
    public IReadOnlyList<ItemPecaOS> ItensPeca => _itensPeca.AsReadOnly();


    public OrdemServico(Guid clienteId, Guid veiculoId)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("O cliente é obrigatório.", nameof(clienteId));

        if (veiculoId == Guid.Empty)
            throw new ArgumentException("O veículo é obrigatório.", nameof(veiculoId));

        Id = Guid.NewGuid();
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOS.Recebida;
    }

    public void CreateDiagnostic()
    {
        if (Status != StatusOS.Recebida)
            throw new InvalidOperationException("O diagnóstico só pode ser iniciado quando a OS estiver recebida.");

        Status = StatusOS.EmDiagnostico;
    }


    public void AdicionarServico(Guid servicoId, int quantidade, decimal precoAplicado)
    {
        if (Status != StatusOS.EmDiagnostico)
            throw new InvalidOperationException("Serviços só podem ser adicionados durante o diagnóstico.");

        var item = new ItemServico(servicoId, quantidade, precoAplicado);

        _itensServico.Add(item);
    }


    public void AdicionarPeca(Guid pecaInsumoId, decimal precoAplicado)
    {
        if (Status != StatusOS.EmDiagnostico)
            throw new InvalidOperationException("Peças só podem ser adicionadas durante o diagnóstico.");

        if (_itensPeca.Any(item => item.PecaInsumoId == pecaInsumoId))
            throw new InvalidOperationException("Essa unidade de peça já foi adicionada à OS.");

        var item = new ItemPecaOS(pecaInsumoId, precoAplicado);

        _itensPeca.Add(item);
    }

    public void GerarOrcamento()
    {
        if (Status != StatusOS.EmDiagnostico)
            throw new InvalidOperationException("O orçamento só pode ser gerado durante o diagnóstico.");

        if (_itensServico.Count == 0 && _itensPeca.Count == 0)
            throw new InvalidOperationException("Não é possível gerar um orçamento sem serviços ou peças.");

        decimal valorServicos = _itensServico.Sum(item => item.CalcularSubtotal());
        decimal valorPecas = _itensPeca.Sum(item => item.PrecoAplicado);

        Orcamento = new Orcamento(valorServicos, valorPecas);
        Status = StatusOS.AguardandoAprovacao;
    }


    public void AprovarOrcamento()
    {
        if (Status != StatusOS.AguardandoAprovacao || Orcamento is null)
            throw new InvalidOperationException("A OS não possui um orçamento aguardando aprovação.");

        Orcamento.Aprovar();
    }

    public void RejeitarOrcamento()
    {
        if (Status != StatusOS.AguardandoAprovacao || Orcamento is null)
            throw new InvalidOperationException("A OS não possui um orçamento aguardando aprovação.");

        Orcamento.Rejeitar();
    }


    public void AddDiagnostic(string descricao)
    {
        if (Status != StatusOS.EmDiagnostico)
            throw new InvalidOperationException("O diagnóstico só pode ser registrado durante a etapa de diagnóstico.");

        if (Diagnostico is not null)
            throw new InvalidOperationException("A Ordem de Serviço já possui um diagnóstico registrado.");

        Diagnostico = new Diagnostico(descricao);
    }



    public void IniciarExecucao()
    {
        if (Status != StatusOS.AguardandoAprovacao)
            throw new InvalidOperationException("A OS precisa estar aguardando aprovação para iniciar a execução.");

        if (Orcamento is null || Orcamento.Status != StatusOrcamento.Aprovado)
            throw new InvalidOperationException("O orçamento precisa estar aprovado para iniciar a execução.");

        DataInicioExecucao = DateTimeOffset.UtcNow;
        Status = StatusOS.EmExecucao;
    }


    public void Finalizar()
    {
        if (Status != StatusOS.EmExecucao)
            throw new InvalidOperationException("A OS só pode ser finalizada durante a execução.");

        DataFinalizacao = DateTimeOffset.UtcNow;
        Status = StatusOS.Finalizada;
    }


    public void Entregar()
    {
        if (Status != StatusOS.Finalizada)
            throw new InvalidOperationException("A OS precisa estar finalizada para ser entregue.");

        Status = StatusOS.Entregue;
    }



}
