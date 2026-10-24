using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.UnitTests.Entities;

public class OrdemServicoTests
{
    [Fact]
    public void DeveCriarOrdemServicoComStatusRecebida()
    {
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var os = new OrdemServico(clienteId, veiculoId);

        Assert.NotEqual(Guid.Empty, os.Id);
        Assert.Equal(clienteId, os.ClienteId);
        Assert.Equal(veiculoId, os.VeiculoId);
        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public void NaoDevePermitirClienteIdVazio()
    {
        Assert.Throws<ArgumentException>(() => new OrdemServico(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void NaoDevePermitirVeiculoIdVazio()
    {
        Assert.Throws<ArgumentException>(() => new OrdemServico(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void DeveCreateDiagnostic()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.CreateDiagnostic();

        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
    }

    [Fact]
    public void NaoDevePermitirCreateDiagnosticDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<InvalidOperationException>(() => os.CreateDiagnostic());
    }


    [Fact]
    public void DeveAdicionarServicoDuranteDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        var servicoId = Guid.NewGuid();

        os.CreateDiagnostic();
        os.AdicionarServico(servicoId, 2, 100m);

        var item = Assert.Single(os.ItensServico);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(servicoId, item.ServicoId);
        Assert.Equal(2, item.Quantidade);
        Assert.Equal(100m, item.PrecoAplicado);
        Assert.Equal(200m, item.CalcularSubtotal());
    }

    [Fact]
    public void NaoDeveAdicionarServicoAntesDoDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.AdicionarServico(Guid.NewGuid(), 1, 100m));

        Assert.Empty(os.ItensServico);
    }

    [Fact]
    public void NaoDeveAdicionarServicoComIdVazio()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AdicionarServico(Guid.Empty, 1, 100m));

        Assert.Empty(os.ItensServico);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NaoDeveAdicionarServicoComQuantidadeInvalida(int quantidade)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AdicionarServico(Guid.NewGuid(), quantidade, 100m));

        Assert.Empty(os.ItensServico);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NaoDeveAdicionarServicoComPrecoInvalido(int preco)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AdicionarServico(Guid.NewGuid(), 1, preco));

        Assert.Empty(os.ItensServico);
    }

    [Fact]
    public void DevePermitirAdicionarMaisDeUmServico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.AdicionarServico(Guid.NewGuid(), 1, 80m);

        Assert.Equal(2, os.ItensServico.Count);
    }


    //===========Testes para Peças na OS===========

    [Fact]
    public void DeveAdicionarPecaDuranteDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        var pecaId = Guid.NewGuid();

        os.CreateDiagnostic();
        os.AdicionarPeca(pecaId, 35m);

        var item = Assert.Single(os.ItensPeca);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(pecaId, item.PecaInsumoId);
        Assert.Equal(35m, item.PrecoAplicado);
    }

    [Fact]
    public void NaoDeveAdicionarPecaAntesDoDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.AdicionarPeca(Guid.NewGuid(), 35m));

        Assert.Empty(os.ItensPeca);
    }

    [Fact]
    public void NaoDeveAdicionarPecaComIdVazio()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AdicionarPeca(Guid.Empty, 35m));

        Assert.Empty(os.ItensPeca);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NaoDeveAdicionarPecaComPrecoInvalido(int preco)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AdicionarPeca(Guid.NewGuid(), preco));

        Assert.Empty(os.ItensPeca);
    }

    [Fact]
    public void NaoDevePermitirMesmaUnidadeDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        var pecaId = Guid.NewGuid();

        os.CreateDiagnostic();
        os.AdicionarPeca(pecaId, 35m);

        Assert.Throws<InvalidOperationException>(() => os.AdicionarPeca(pecaId, 35m));

        Assert.Single(os.ItensPeca);
    }

    [Fact]
    public void DevePermitirDuasUnidadesDiferentes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        os.AdicionarPeca(Guid.NewGuid(), 35m);
        os.AdicionarPeca(Guid.NewGuid(), 35m);

        Assert.Equal(2, os.ItensPeca.Count);
    }

    //===========Testes para Orçamento===========

    [Fact]
    public void DeveGerarOrcamentoComServicosEPecas()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        os.AdicionarServico(Guid.NewGuid(), 2, 100m);
        os.AdicionarPeca(Guid.NewGuid(), 35m);

        os.GerarOrcamento();

        Assert.NotNull(os.Orcamento);
        Assert.NotEqual(Guid.Empty, os.Orcamento.Id);
        Assert.Equal(200m, os.Orcamento.ValorServicos);
        Assert.Equal(35m, os.Orcamento.ValorPecas);
        Assert.Equal(235m, os.Orcamento.ValorTotal);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamento.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void DeveGerarOrcamentoApenasComServicos()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 120m);

        os.GerarOrcamento();

        Assert.NotNull(os.Orcamento);
        Assert.Equal(120m, os.Orcamento.ValorServicos);
        Assert.Equal(0m, os.Orcamento.ValorPecas);
        Assert.Equal(120m, os.Orcamento.ValorTotal);
    }

    [Fact]
    public void DeveGerarOrcamentoApenasComPecas()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarPeca(Guid.NewGuid(), 35m);

        os.GerarOrcamento();

        Assert.NotNull(os.Orcamento);
        Assert.Equal(0m, os.Orcamento.ValorServicos);
        Assert.Equal(35m, os.Orcamento.ValorPecas);
        Assert.Equal(35m, os.Orcamento.ValorTotal);
    }

    [Fact]
    public void NaoDeveGerarOrcamentoAntesDoDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.GerarOrcamento());

        Assert.Null(os.Orcamento);
        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public void NaoDeveGerarOrcamentoSemItens()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<InvalidOperationException>(() => os.GerarOrcamento());

        Assert.Null(os.Orcamento);
        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
    }

    [Fact]
    public void NaoDeveGerarOrcamentoDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        var orcamentoId = os.Orcamento!.Id;

        Assert.Throws<InvalidOperationException>(() => os.GerarOrcamento());

        Assert.Equal(orcamentoId, os.Orcamento.Id);
    }

    [Fact]
    public void NaoDeveAdicionarServicoAposGerarOrcamento()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.AdicionarServico(Guid.NewGuid(), 1, 50m));

        Assert.Single(os.ItensServico);
        Assert.Equal(100m, os.Orcamento!.ValorTotal);
    }

    [Fact]
    public void NaoDeveAdicionarPecaAposGerarOrcamento()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarPeca(Guid.NewGuid(), 35m);
        os.GerarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.AdicionarPeca(Guid.NewGuid(), 25m));

        Assert.Single(os.ItensPeca);
        Assert.Equal(35m, os.Orcamento!.ValorTotal);
    }


    [Fact]
    public void DeveAprovarOrcamentoPendente()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        os.AprovarOrcamento();

        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
    }

    [Fact]
    public void DeveRejeitarOrcamentoPendente()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        os.RejeitarOrcamento();

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
    }

    [Fact]
    public void NaoDeveAprovarOrcamentoInexistente()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.AprovarOrcamento());

        Assert.Null(os.Orcamento);
    }

    [Fact]
    public void NaoDeveRejeitarOrcamentoInexistente()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.RejeitarOrcamento());

        Assert.Null(os.Orcamento);
    }

    [Fact]
    public void NaoDeveAprovarOrcamentoDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();
        os.AprovarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.AprovarOrcamento());

        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
    }

    [Fact]
    public void NaoDeveRejeitarOrcamentoDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();
        os.RejeitarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.RejeitarOrcamento());

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
    }

    [Fact]
    public void NaoDeveAprovarOrcamentoRejeitado()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();
        os.RejeitarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.AprovarOrcamento());

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
    }

    [Fact]
    public void NaoDeveRejeitarOrcamentoAprovado()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();
        os.AprovarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.RejeitarOrcamento());

        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
    }


    //===Testes sobre Diagnostico====


    [Fact]
    public void DeveAddDiagnostic()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        os.AddDiagnostic("Desgaste nas pastilhas de freio.");

        Assert.NotNull(os.Diagnostico);
        Assert.NotEqual(Guid.Empty, os.Diagnostico.Id);
        Assert.Equal("Desgaste nas pastilhas de freio.", os.Diagnostico.Descricao);
        Assert.NotEqual(default, os.Diagnostico.DataRegistro);
        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NaoDeveAddDiagnosticSemDescricao(string? descricao)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        Assert.Throws<ArgumentException>(() => os.AddDiagnostic(descricao!));

        Assert.Null(os.Diagnostico);
    }

    [Fact]
    public void NaoDeveAddDiagnosticAntesDeIniciar()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.AddDiagnostic("Desgaste nas pastilhas."));

        Assert.Null(os.Diagnostico);
        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public void NaoDeveAddDiagnosticDuasVezes()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AddDiagnostic("Primeiro diagnóstico.");

        var diagnosticoId = os.Diagnostico!.Id;

        Assert.Throws<InvalidOperationException>(() => os.AddDiagnostic("Segundo diagnóstico."));

        Assert.Equal(diagnosticoId, os.Diagnostico.Id);
        Assert.Equal("Primeiro diagnóstico.", os.Diagnostico.Descricao);
    }

    [Fact]
    public void NaoDeveAddDiagnosticAposGerarOrcamento()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.AddDiagnostic("Diagnóstico tardio."));

        Assert.Null(os.Diagnostico);
    }

    [Fact]
    public void DeveRemoverEspacosDaDescricaoDoDiagnostico()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();

        os.AddDiagnostic("  Desgaste nas pastilhas.  ");

        Assert.Equal("Desgaste nas pastilhas.", os.Diagnostico!.Descricao);
    }

    [Fact]
    public void DevePermitirGerarOrcamentoAposAddDiagnostic()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.CreateDiagnostic();
        os.AddDiagnostic("Necessária troca do filtro de óleo.");
        os.AdicionarServico(Guid.NewGuid(), 1, 120m);
        os.AdicionarPeca(Guid.NewGuid(), 35m);

        os.GerarOrcamento();

        Assert.NotNull(os.Diagnostico);
        Assert.NotNull(os.Orcamento);
        Assert.Equal(155m, os.Orcamento.ValorTotal);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
    }

    //=====Etapas da OS=====


    private static OrdemServico CriarOsComOrcamento()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        return os;
    }



    [Fact]
    public void DeveIniciarExecucaoComOrcamentoAprovado()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();

        os.IniciarExecucao();

        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.NotNull(os.DataInicioExecucao);
        Assert.Null(os.DataFinalizacao);
    }

    [Fact]
    public void NaoDeveIniciarExecucaoComOrcamentoPendente()
    {
        var os = CriarOsComOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao());

        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Null(os.DataInicioExecucao);
    }

    [Fact]
    public void NaoDeveIniciarExecucaoComOrcamentoRejeitado()
    {
        var os = CriarOsComOrcamento();
        os.RejeitarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao());

        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Null(os.DataInicioExecucao);
    }

    [Fact]
    public void NaoDeveIniciarExecucaoAntesDeGerarOrcamento()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao());

        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public void NaoDeveIniciarExecucaoDuasVezes()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();

        var dataInicial = os.DataInicioExecucao;

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao());

        Assert.Equal(dataInicial, os.DataInicioExecucao);
    }



    [Fact]
    public void DeveFinalizarOsEmExecucao()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();

        os.Finalizar();

        Assert.Equal(StatusOS.Finalizada, os.Status);
        Assert.NotNull(os.DataFinalizacao);
        Assert.True(os.DataFinalizacao >= os.DataInicioExecucao);
    }

    [Fact]
    public void NaoDeveFinalizarOsAntesDaExecucao()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.Finalizar());

        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Null(os.DataFinalizacao);
    }

    [Fact]
    public void NaoDeveFinalizarOsDuasVezes()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();
        os.Finalizar();

        var dataFinalizacao = os.DataFinalizacao;

        Assert.Throws<InvalidOperationException>(() => os.Finalizar());

        Assert.Equal(dataFinalizacao, os.DataFinalizacao);
    }


    [Fact]
    public void DeveEntregarOsFinalizada()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();
        os.Finalizar();

        os.Entregar();

        Assert.Equal(StatusOS.Entregue, os.Status);
    }

    [Fact]
    public void NaoDeveEntregarOsAntesDaFinalizacao()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();

        Assert.Throws<InvalidOperationException>(() => os.Entregar());

        Assert.Equal(StatusOS.EmExecucao, os.Status);
    }

    [Fact]
    public void NaoDeveEntregarOsDuasVezes()
    {
        var os = CriarOsComOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();
        os.Finalizar();
        os.Entregar();

        Assert.Throws<InvalidOperationException>(() => os.Entregar());

        Assert.Equal(StatusOS.Entregue, os.Status);
    }



    //=======Testes de fluxos completos========

    [Fact]
    public void DeveCompletarFluxoDeOrdemServico()
    {
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = new OrdemServico(clienteId, veiculoId);

        // Diagnóstico
        os.CreateDiagnostic();
        os.AddDiagnostic("Necessária troca do óleo e do filtro.");

        // Serviços e peças
        os.AdicionarServico(Guid.NewGuid(), 1, 120m);
        os.AdicionarPeca(peca.Id, peca.Preco);

        // Orçamento
        os.GerarOrcamento();
        Assert.Equal(155m, os.Orcamento!.ValorTotal);

        // Aprovação e reserva
        os.AprovarOrcamento();
        peca.Reservar(os.Id);

        // Execução
        os.IniciarExecucao();
        peca.Consumir(os.Id);

        // Finalização e entrega
        os.Finalizar();
        os.Entregar();

        Assert.Equal(StatusOS.Entregue, os.Status);
        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento.Status);
        Assert.Equal(StatusPecaInsumo.Consumida, peca.Status);
        Assert.Equal(os.Id, peca.OrdemServicoId);
        Assert.NotNull(os.DataInicioExecucao);
        Assert.NotNull(os.DataFinalizacao);
    }



    [Fact]
    public void NaoDeveExecutarOrdemServicoComOrcamentoRejeitado()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.CreateDiagnostic();
        os.AdicionarPeca(peca.Id, peca.Preco);
        os.GerarOrcamento();

        os.RejeitarOrcamento();

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao());

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Null(os.DataInicioExecucao);
    }



}
