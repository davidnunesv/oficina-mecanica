
using Oficina.Application.Interfaces;
using Oficina.Application.Services;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.Services;

public class OrdemServicoApplicationServiceTests
{
    [Fact]
    public async Task DeveAbrirOsParaVeiculoDoCliente()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        var os = Assert.Single(repositorio.OrdensServico);

        Assert.Equal(osId, os.Id);
        Assert.Equal(cliente.Id, os.ClienteId);
        Assert.Equal(veiculo.Id, os.VeiculoId);
        Assert.Equal(StatusOS.Recebida, os.Status);
    }

    [Fact]
    public async Task NaoDeveAbrirOsComClienteInexistente()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto(incluirCliente: false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(cliente.Id, veiculo.Id));

        Assert.Empty(repositorio.OrdensServico);
    }

    [Fact]
    public async Task NaoDeveAbrirOsComVeiculoInexistente()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto(incluirVeiculo: false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(cliente.Id, veiculo.Id));

        Assert.Empty(repositorio.OrdensServico);
    }

    [Fact]
    public async Task NaoDeveAbrirOsComVeiculoDeOutroCliente()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto(veiculoDeOutroCliente: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(cliente.Id, veiculo.Id));

        Assert.Empty(repositorio.OrdensServico);
    }

    [Fact]
    public async Task NaoDeveAbrirOsComIdentificadoresVazios()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Guid.Empty, veiculo.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(cliente.Id, Guid.Empty));

        Assert.Empty(repositorio.OrdensServico);
    }

    [Fact]
    public async Task DeveConsultarOsPorId()
    {
        var (service, _, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        var resultado = await service.GetByIdAsync(osId);

        Assert.NotNull(resultado);
        Assert.Equal(osId, resultado.Id);
        Assert.Equal(cliente.Id, resultado.ClienteId);
        Assert.Equal(veiculo.Id, resultado.VeiculoId);
        Assert.Equal("Recebida", resultado.Status);
        Assert.Null(resultado.StatusOrcamento);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoOsNaoExiste()
    {
        var (service, _, _, _) = CriarContexto();

        var resultado = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task NaoDeveConsultarOsComIdVazio()
    {
        var (service, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task DeveListarOrdensServico()
    {
        var (service, _, cliente, veiculo) = CriarContexto();

        var primeiraId = await service.CreateAsync(cliente.Id, veiculo.Id);
        var segundaId = await service.CreateAsync(cliente.Id, veiculo.Id);

        var resultado = await service.ListAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, os => os.Id == primeiraId);
        Assert.Contains(resultado, os => os.Id == segundaId);
    }

    [Fact]
    public async Task DeveRetornarListaVaziaQuandoNaoExistemOrdensServico()
    {
        var (service, _, _, _) = CriarContexto();

        var resultado = await service.ListAsync();

        Assert.Empty(resultado);
    }


    [Fact]
    public async Task DeveIniciarDiagnostico()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        var iniciado = await service.StartDiagnosisAsync(osId);

        var os = Assert.Single(repositorio.OrdensServico);

        Assert.True(iniciado);
        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
    }

    [Fact]
    public async Task NaoDeveIniciarDiagnosticoDeOsInexistente()
    {
        var (service, _, _, _) = CriarContexto();

        var iniciado = await service.StartDiagnosisAsync(Guid.NewGuid());

        Assert.False(iniciado);
    }

    [Fact]
    public async Task NaoDeveIniciarDiagnosticoDuasVezes()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await service.StartDiagnosisAsync(osId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartDiagnosisAsync(osId));

        Assert.Equal(StatusOS.EmDiagnostico, Assert.Single(repositorio.OrdensServico).Status);
    }


    [Fact]
    public async Task DeveRegistrarDiagnostico()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await service.StartDiagnosisAsync(osId);

        var registrado = await service.AddDiagnosisAsync(osId, "  Desgaste nas pastilhas de freio.  ");

        var os = Assert.Single(repositorio.OrdensServico);

        Assert.True(registrado);
        Assert.NotNull(os.Diagnostico);
        Assert.Equal("Desgaste nas pastilhas de freio.", os.Diagnostico.Descricao);
        Assert.NotEqual(default, os.Diagnostico.DataRegistro);
        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
    }

    [Fact]
    public async Task NaoDeveRegistrarDiagnosticoAntesDeIniciar()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddDiagnosisAsync(osId, "Desgaste nas pastilhas."));

        Assert.Null(Assert.Single(repositorio.OrdensServico).Diagnostico);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task NaoDeveRegistrarDiagnosticoSemDescricao(string? descricao)
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await service.StartDiagnosisAsync(osId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddDiagnosisAsync(osId, descricao!));

        Assert.Null(Assert.Single(repositorio.OrdensServico).Diagnostico);
    }

    [Fact]
    public async Task NaoDeveRegistrarDiagnosticoDuasVezes()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await service.StartDiagnosisAsync(osId);
        await service.AddDiagnosisAsync(osId, "Primeiro diagnóstico.");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddDiagnosisAsync(osId, "Segundo diagnóstico."));

        var os = Assert.Single(repositorio.OrdensServico);

        Assert.Equal("Primeiro diagnóstico.", os.Diagnostico!.Descricao);
    }

    [Fact]
    public async Task NaoDeveRegistrarDiagnosticoEmOsInexistente()
    {
        var (service, _, _, _) = CriarContexto();

        var registrado = await service.AddDiagnosisAsync(Guid.NewGuid(), "Descrição");

        Assert.False(registrado);
    }

    [Fact]
    public async Task NaoDeveAceitarIdVazioNasOperacoesDeDiagnostico()
    {
        var (service, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.StartDiagnosisAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddDiagnosisAsync(Guid.Empty, "Descrição"));
    }


    [Fact]
    public async Task DeveAdicionarServicoComPrecoDoCatalogo()
    {
        var catalogo = new FakeServicoRepository();
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 120m);
        catalogo.Servicos.Add(servico);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        var adicionado = await app.AddServiceAsync(os.Id, servico.Id, 2);

        var item = Assert.Single(os.ItensServico);

        Assert.True(adicionado);
        Assert.Equal(servico.Id, item.ServicoId);
        Assert.Equal(2, item.Quantidade);
        Assert.Equal(120m, item.PrecoAplicado);
        Assert.Equal(240m, item.CalcularSubtotal());
        Assert.Equal(1, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAdicionarServicoInexistente()
    {
        var (app, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => app.AddServiceAsync(os.Id, Guid.NewGuid(), 1));

        Assert.Empty(os.ItensServico);
        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAdicionarServicoAntesDoDiagnostico()
    {
        var catalogo = new FakeServicoRepository();
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);
        catalogo.Servicos.Add(servico);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogo);
        var osId = await app.CreateAsync(cliente.Id, veiculo.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.AddServiceAsync(osId, servico.Id, 1));

        Assert.Empty(Assert.Single(repositorio.OrdensServico).ItensServico);
        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAdicionarServicoComQuantidadeInvalida()
    {
        var catalogo = new FakeServicoRepository();
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);
        catalogo.Servicos.Add(servico);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await Assert.ThrowsAsync<ArgumentException>(() => app.AddServiceAsync(os.Id, servico.Id, 0));

        Assert.Empty(os.ItensServico);
    }



    [Fact]
    public async Task DeveAdicionarPecaSemReserva()
    {
        var catalogo = new FakePecaInsumoRepository();
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        catalogo.Pecas.Add(peca);

        var (app, repositorio, cliente, veiculo) = CriarContexto(pecas: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        var adicionado = await app.AddPecaAsync(os.Id, peca.Id);

        var item = Assert.Single(os.ItensPeca);

        Assert.True(adicionado);
        Assert.Equal(peca.Id, item.PecaInsumoId);
        Assert.Equal(35m, item.PrecoAplicado);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Null(peca.OrdemServicoId);
    }

    [Fact]
    public async Task NaoDeveAdicionarPecaInexistente()
    {
        var (app, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => app.AddPecaAsync(os.Id, Guid.NewGuid()));

        Assert.Empty(os.ItensPeca);
    }

    [Fact]
    public async Task NaoDeveAdicionarPecaReservada()
    {
        var catalogo = new FakePecaInsumoRepository();
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        peca.Reservar(Guid.NewGuid());
        catalogo.Pecas.Add(peca);

        var (app, repositorio, cliente, veiculo) = CriarContexto(pecas: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.AddPecaAsync(os.Id, peca.Id));

        Assert.Empty(os.ItensPeca);
    }

    [Fact]
    public async Task NaoDeveAdicionarMesmaUnidadeDuasVezes()
    {
        var catalogo = new FakePecaInsumoRepository();
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        catalogo.Pecas.Add(peca);

        var (app, repositorio, cliente, veiculo) = CriarContexto(pecas: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await app.AddPecaAsync(os.Id, peca.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.AddPecaAsync(os.Id, peca.Id));

        Assert.Single(os.ItensPeca);
        Assert.Equal(1, repositorio.UpdateCount);
    }


    [Fact]
    public async Task DeveGerarOrcamentoComServicosEPecas()
    {
        var catalogoServicos = new FakeServicoRepository();
        var catalogoPecas = new FakePecaInsumoRepository();

        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 120m);
        var peca = new PecaInsumo("Filtro de óleo", 35m);

        catalogoServicos.Servicos.Add(servico);
        catalogoPecas.Pecas.Add(peca);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogoServicos, pecas: catalogoPecas);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await app.AddServiceAsync(os.Id, servico.Id, 2);
        await app.AddPecaAsync(os.Id, peca.Id);

        var gerado = await app.GenerateOrcamentoAsync(os.Id);

        Assert.True(gerado);
        Assert.NotNull(os.Orcamento);
        Assert.Equal(240m, os.Orcamento.ValorServicos);
        Assert.Equal(35m, os.Orcamento.ValorPecas);
        Assert.Equal(275m, os.Orcamento.ValorTotal);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamento.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Equal(3, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveGerarOrcamentoSemItens()
    {
        var (app, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.GenerateOrcamentoAsync(os.Id));

        Assert.Null(os.Orcamento);
        Assert.Equal(StatusOS.EmDiagnostico, os.Status);
        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveGerarOrcamentoDuasVezes()
    {
        var catalogo = new FakeServicoRepository();
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);
        catalogo.Servicos.Add(servico);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await app.AddServiceAsync(os.Id, servico.Id, 1);
        await app.GenerateOrcamentoAsync(os.Id);

        var orcamentoId = os.Orcamento!.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.GenerateOrcamentoAsync(os.Id));

        Assert.Equal(orcamentoId, os.Orcamento.Id);
        Assert.Equal(2, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAdicionarServicoAposGerarOrcamento()
    {
        var catalogo = new FakeServicoRepository();
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);
        catalogo.Servicos.Add(servico);

        var (app, repositorio, cliente, veiculo) = CriarContexto(servicos: catalogo);
        var os = await CriarOsEmDiagnosticoAsync(repositorio, cliente, veiculo);

        await app.AddServiceAsync(os.Id, servico.Id, 1);
        await app.GenerateOrcamentoAsync(os.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => app.AddServiceAsync(os.Id, servico.Id, 1));

        Assert.Single(os.ItensServico);
        Assert.Equal(100m, os.Orcamento!.ValorTotal);
    }

    [Fact]
    public async Task DeveRetornarFalsoQuandoOsNaoExiste()
    {
        var (app, _, _, _) = CriarContexto();
        var osId = Guid.NewGuid();

        Assert.False(await app.AddServiceAsync(osId, Guid.NewGuid(), 1));
        Assert.False(await app.AddPecaAsync(osId, Guid.NewGuid()));
        Assert.False(await app.GenerateOrcamentoAsync(osId));
    }

    [Fact]
    public async Task NaoDeveAceitarIdentificadoresVazios()
    {
        var (app, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => app.AddServiceAsync(Guid.Empty, Guid.NewGuid(), 1));
        await Assert.ThrowsAsync<ArgumentException>(() => app.AddServiceAsync(Guid.NewGuid(), Guid.Empty, 1));
        await Assert.ThrowsAsync<ArgumentException>(() => app.AddPecaAsync(Guid.Empty, Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => app.AddPecaAsync(Guid.NewGuid(), Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => app.GenerateOrcamentoAsync(Guid.Empty));
    }


    [Fact]
    public async Task DeveRejeitarOrcamentoPendente()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsComOrcamentoPendenteAsync(repositorio, cliente, veiculo);

        var rejeitado = await service.RejeitarOrcamentoAsync(os.Id);

        Assert.True(rejeitado);
        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(1, repositorio.UpdateCount);
        Assert.Null(os.DataInicioExecucao);
    }

    [Fact]
    public async Task NaoDeveRejeitarOrcamentoDuasVezes()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsComOrcamentoPendenteAsync(repositorio, cliente, veiculo);

        await service.RejeitarOrcamentoAsync(os.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejeitarOrcamentoAsync(os.Id));

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
        Assert.Equal(1, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveRejeitarOrcamentoAprovado()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsComOrcamentoPendenteAsync(repositorio, cliente, veiculo);
        os.AprovarOrcamento();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejeitarOrcamentoAsync(os.Id));

        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveRejeitarOsSemOrcamento()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejeitarOrcamentoAsync(osId));

        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveRejeitarOrcamentoDeOsInexistente()
    {
        var (service, _, _, _) = CriarContexto();

        var rejeitado = await service.RejeitarOrcamentoAsync(Guid.NewGuid());

        Assert.False(rejeitado);
    }

    [Fact]
    public async Task NaoDeveRejeitarOrcamentoComIdVazio()
    {
        var (service, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.RejeitarOrcamentoAsync(Guid.Empty));
    }



    [Fact]
    public async Task DeveEntregarVeiculoDeOsFinalizada()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        var entregue = await service.Entregar(os.Id);

        Assert.True(entregue);
        Assert.Equal(StatusOS.Entregue, os.Status);
        Assert.NotNull(os.DataFinalizacao);
        Assert.Equal(1, repositorio.UpdateCount);

        var consulta = await service.GetByIdAsync(os.Id);

        Assert.NotNull(consulta);
        Assert.Equal("Entregue", consulta.Status);
    }

    [Fact]
    public async Task NaoDeveEntregarVeiculoAntesDeFinalizarOs()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Entregar(osId));

        var os = Assert.Single(repositorio.OrdensServico);

        Assert.Equal(StatusOS.Recebida, os.Status);
        Assert.Equal(0, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveEntregarVeiculoDuasVezes()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        await service.Entregar(os.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Entregar(os.Id));

        Assert.Equal(StatusOS.Entregue, os.Status);
        Assert.Equal(1, repositorio.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveEntregarVeiculoDeOsInexistente()
    {
        var (service, _, _, _) = CriarContexto();

        var entregue = await service.Entregar(Guid.NewGuid());

        Assert.False(entregue);
    }

    [Fact]
    public async Task NaoDeveEntregarVeiculoComIdVazio()
    {
        var (service, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.Entregar(Guid.Empty));
    }



    [Fact]
    public async Task DeveConsultarDetalhesComDiagnosticoServicosPecasEOrcamento()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        var os = new OrdemServico(cliente.Id, veiculo.Id);
        var servicoId = Guid.NewGuid();
        var pecaId = Guid.NewGuid();

        os.CreateDiagnostic();
        os.AddDiagnostic("Desgaste nas pastilhas de freio.");
        os.AdicionarServico(servicoId, 2, 120m);
        os.AdicionarPeca(pecaId, 35m);
        os.GerarOrcamento();

        await repositorio.CreateAsync(os);

        var detalhe = await service.GetDetailsByIdAsync(os.Id);

        Assert.NotNull(detalhe);
        Assert.Equal(os.Id, detalhe.Id);
        Assert.Equal(cliente.Id, detalhe.ClienteId);
        Assert.Equal(veiculo.Id, detalhe.VeiculoId);
        Assert.Equal("AguardandoAprovacao", detalhe.Status);

        Assert.NotNull(detalhe.Diagnostico);
        Assert.Equal("Desgaste nas pastilhas de freio.", detalhe.Diagnostico.Descricao);
        Assert.NotEqual(default, detalhe.Diagnostico.DataRegistro);

        var itemServico = Assert.Single(detalhe.Servicos);

        Assert.Equal(servicoId, itemServico.ServicoId);
        Assert.Equal(2, itemServico.Quantidade);
        Assert.Equal(120m, itemServico.PrecoAplicado);
        Assert.Equal(240m, itemServico.Subtotal);

        var itemPeca = Assert.Single(detalhe.Pecas);

        Assert.Equal(pecaId, itemPeca.PecaInsumoId);
        Assert.Equal(35m, itemPeca.PrecoAplicado);

        Assert.NotNull(detalhe.Orcamento);
        Assert.Equal(os.Orcamento!.Id, detalhe.Orcamento.Id);
        Assert.Equal(240m, detalhe.Orcamento.ValorServicos);
        Assert.Equal(35m, detalhe.Orcamento.ValorPecas);
        Assert.Equal(275m, detalhe.Orcamento.ValorTotal);
        Assert.Equal("Pendente", detalhe.Orcamento.Status);

        Assert.Null(detalhe.DataInicioExecucao);
        Assert.Null(detalhe.DataFinalizacao);
        Assert.Equal(0, repositorio.UpdateCount);
    }


    [Fact]
    public async Task DeveConsultarDetalhesDeOsSemDiagnosticoEOrcamento()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var osId = await service.CreateAsync(cliente.Id, veiculo.Id);

        var detalhe = await service.GetDetailsByIdAsync(osId);

        Assert.NotNull(detalhe);
        Assert.Equal("Recebida", detalhe.Status);
        Assert.Null(detalhe.Diagnostico);
        Assert.Empty(detalhe.Servicos);
        Assert.Empty(detalhe.Pecas);
        Assert.Null(detalhe.Orcamento);
        Assert.Null(detalhe.DataInicioExecucao);
        Assert.Null(detalhe.DataFinalizacao);
        Assert.Equal(0, repositorio.UpdateCount);
    }



    [Fact]
    public async Task DeveConsultarDetalhesDeOsEntregue()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        await service.Entregar(os.Id);

        var detalhe = await service.GetDetailsByIdAsync(os.Id);

        Assert.NotNull(detalhe);
        Assert.Equal("Entregue", detalhe.Status);
        Assert.Equal("Aprovado", detalhe.Orcamento!.Status);
        Assert.NotNull(detalhe.DataInicioExecucao);
        Assert.NotNull(detalhe.DataFinalizacao);
        Assert.True(detalhe.DataFinalizacao >= detalhe.DataInicioExecucao);
    }


    [Fact]
    public async Task DeveRetornarNuloAoConsultarDetalhesDeOsInexistente()
    {
        var (service, _, _, _) = CriarContexto();

        var detalhe = await service.GetDetailsByIdAsync(Guid.NewGuid());

        Assert.Null(detalhe);
    }

    [Fact]
    public async Task NaoDeveConsultarDetalhesComIdVazio()
    {
        var (service, _, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetDetailsByIdAsync(Guid.Empty));
    }


    [Fact]
    public async Task DeveRetornarMediaNulaQuandoNaoExistemOrdens()
    {
        var (service, repositorio, _, _) = CriarContexto();

        var resultado = await service.GetTempoMedioExecucaoAsync();

        Assert.Equal(0, resultado.QuantidadeOrdens);
        Assert.Null(resultado.TempoMedio);
        Assert.Equal(0, repositorio.UpdateCount);
    }


    [Fact]
    public async Task DeveCalcularTempoDeUmaOsFinalizada()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();
        var os = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        var resultado = await service.GetTempoMedioExecucaoAsync();

        var duracaoEsperada = os.DataFinalizacao!.Value - os.DataInicioExecucao!.Value;

        Assert.Equal(1, resultado.QuantidadeOrdens);
        Assert.NotNull(resultado.TempoMedio);
        Assert.Equal(duracaoEsperada, resultado.TempoMedio.Value);
        Assert.Equal(0, repositorio.UpdateCount);
    }



    [Fact]
    public async Task DeveCalcularMediaDeOrdensFinalizadasEEntregues()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        var primeiraOs = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);
        var segundaOs = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        segundaOs.Entregar();

        var resultado = await service.GetTempoMedioExecucaoAsync();

        var primeiraDuracao = primeiraOs.DataFinalizacao!.Value - primeiraOs.DataInicioExecucao!.Value;
        var segundaDuracao = segundaOs.DataFinalizacao!.Value - segundaOs.DataInicioExecucao!.Value;
        var mediaEsperada = TimeSpan.FromTicks((long)new[] { primeiraDuracao.Ticks, segundaDuracao.Ticks }.Average());

        Assert.Equal(2, resultado.QuantidadeOrdens);
        Assert.NotNull(resultado.TempoMedio);
        Assert.Equal(mediaEsperada, resultado.TempoMedio.Value);
        Assert.Equal(StatusOS.Entregue, segundaOs.Status);
        Assert.Equal(0, repositorio.UpdateCount);
    }



    [Fact]
    public async Task NaoDeveConsiderarOrdensNaoFinalizadas()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        var osRecebida = new OrdemServico(cliente.Id, veiculo.Id);
        await repositorio.CreateAsync(osRecebida);

        var osEmExecucao = new OrdemServico(cliente.Id, veiculo.Id);
        osEmExecucao.CreateDiagnostic();
        osEmExecucao.AdicionarServico(Guid.NewGuid(), 1, 100m);
        osEmExecucao.GerarOrcamento();
        osEmExecucao.AprovarOrcamento();
        osEmExecucao.IniciarExecucao();

        await repositorio.CreateAsync(osEmExecucao);

        var resultado = await service.GetTempoMedioExecucaoAsync();

        Assert.Equal(0, resultado.QuantidadeOrdens);
        Assert.Null(resultado.TempoMedio);
        Assert.Equal(StatusOS.EmExecucao, osEmExecucao.Status);
        Assert.Equal(0, repositorio.UpdateCount);
    }


    [Fact]
    public async Task DeveConsiderarSomenteOrdensConcluidasNaMedia()
    {
        var (service, repositorio, cliente, veiculo) = CriarContexto();

        var osFinalizada = await CriarOsFinalizadaAsync(repositorio, cliente, veiculo);

        var osRecebida = new OrdemServico(cliente.Id, veiculo.Id);
        await repositorio.CreateAsync(osRecebida);

        var osRejeitada = new OrdemServico(cliente.Id, veiculo.Id);
        osRejeitada.CreateDiagnostic();
        osRejeitada.AdicionarServico(Guid.NewGuid(), 1, 100m);
        osRejeitada.GerarOrcamento();
        osRejeitada.RejeitarOrcamento();

        await repositorio.CreateAsync(osRejeitada);

        var resultado = await service.GetTempoMedioExecucaoAsync();

        var duracaoEsperada = osFinalizada.DataFinalizacao!.Value - osFinalizada.DataInicioExecucao!.Value;

        Assert.Equal(1, resultado.QuantidadeOrdens);
        Assert.NotNull(resultado.TempoMedio);
        Assert.Equal(duracaoEsperada, resultado.TempoMedio.Value);
        Assert.Equal(0, repositorio.UpdateCount);
    }


    private static (OrdemServicoApplicationService Service, FakeOrdemServicoRepository Repositorio, Cliente Cliente, Veiculo Veiculo) CriarContexto(
    bool incluirCliente = true,
    bool incluirVeiculo = true,
    bool veiculoDeOutroCliente = false,
    FakeServicoRepository? servicos = null,
    FakePecaInsumoRepository? pecas = null)
    {
        var cliente = new Cliente("João Silva", new Documento("52998224725"));
        var proprietarioId = veiculoDeOutroCliente ? Guid.NewGuid() : cliente.Id;
        var veiculo = new Veiculo(proprietarioId, new Placa("ABC1234"), "Ford", "Ka", 2019);

        var clienteRepository = new FakeClienteRepository(incluirCliente ? cliente : null);
        var veiculoRepository = new FakeVeiculoRepository(incluirVeiculo ? veiculo : null);
        var ordemServicoRepository = new FakeOrdemServicoRepository();

        var service = new OrdemServicoApplicationService(
            ordemServicoRepository, clienteRepository, veiculoRepository,
            servicos ?? new FakeServicoRepository(), pecas ?? new FakePecaInsumoRepository());

        return (service, ordemServicoRepository, cliente, veiculo);
    }


    private static async Task<OrdemServico> CriarOsEmDiagnosticoAsync(FakeOrdemServicoRepository repositorio, Cliente cliente, Veiculo veiculo)
    {
        var os = new OrdemServico(cliente.Id, veiculo.Id);
        os.CreateDiagnostic();

        await repositorio.CreateAsync(os);

        return os;
    }



    private sealed class FakeOrdemServicoRepository : IOrdemServicoRepository
    {
        private readonly List<OrdemServico> _ordensServico = new();

        public IReadOnlyList<OrdemServico> OrdensServico => _ordensServico.AsReadOnly();

        public Task CreateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
        {
            _ordensServico.Add(ordemServico);
            return Task.CompletedTask;
        }

        public Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_ordensServico.FirstOrDefault(os => os.Id == id));
        }

        public Task<IReadOnlyList<OrdemServico>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OrdemServico>>(_ordensServico.ToList());
        }

        public int UpdateCount { get; private set; }
        public Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

    }


    private static async Task<OrdemServico> CriarOsComOrcamentoPendenteAsync(FakeOrdemServicoRepository repositorio, Cliente cliente, Veiculo veiculo)
    {
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();

        await repositorio.CreateAsync(os);

        return os;
    }


    private static async Task<OrdemServico> CriarOsFinalizadaAsync(FakeOrdemServicoRepository repositorio, Cliente cliente, Veiculo veiculo)
    {
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);
        os.GerarOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();
        os.Finalizar();

        await repositorio.CreateAsync(os);

        return os;
    }



    private sealed class FakeClienteRepository(Cliente? cliente) : IClienteRepository
    {
        public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Cliente?>(cliente?.Id == id ? cliente : null);
        }

        public Task<bool> HasDocumentoAsync(Documento documento, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CreateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid clienteId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeVeiculoRepository(Veiculo? veiculo) : IVeiculoRepository
    {
        public Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Veiculo?>(veiculo?.Id == id ? veiculo : null);
        }

        public Task<bool> HasPlacaAsync(Placa placa, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CreateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Veiculo>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid veiculoId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }


    private sealed class FakeServicoRepository : IServicoRepository
    {
        public List<Servico> Servicos { get; } = new();

        public Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Servicos.FirstOrDefault(servico => servico.Id == id));
        }

        public Task CreateAsync(Servico servico, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Servico>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Servico servico, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid servicoId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakePecaInsumoRepository : IPecaInsumoRepository
    {
        public List<PecaInsumo> Pecas { get; } = new();

        public Task<PecaInsumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Pecas.FirstOrDefault(peca => peca.Id == id));
        }

        public Task CreateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<PecaInsumo>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid pecaId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }


}
