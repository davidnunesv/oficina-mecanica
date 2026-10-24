
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Services;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Repositories;
using Oficina.IntegrationTests.Persistence;
using Xunit;

namespace Oficina.IntegrationTests.Flows;

[Collection("SqlServerIntegration")]
public class FluxoOficinaIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DeveExecutarFluxoCompletoAteEntrega()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            // Etapa 1: diagnóstico, itens e orçamento.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                Assert.True(await app.Ordens.StartDiagnosisAsync(cenario.OsId));
                Assert.True(await app.Ordens.AddDiagnosisAsync(cenario.OsId, "Necessária manutenção preventiva."));
                Assert.True(await app.Ordens.AddServiceAsync(cenario.OsId, cenario.ServicoId, 2));
                Assert.True(await app.Ordens.AddPecaAsync(cenario.OsId, cenario.PecaId));
                Assert.True(await app.Ordens.GenerateOrcamentoAsync(cenario.OsId));
            }

            // Etapa 2: verificar orçamento persistido.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                var detalhes = await app.Ordens.GetDetailsByIdAsync(cenario.OsId);

                Assert.NotNull(detalhes);
                Assert.Equal("AguardandoAprovacao", detalhes.Status);
                Assert.Equal("Necessária manutenção preventiva.", detalhes.Diagnostico!.Descricao);

                var servico = Assert.Single(detalhes.Servicos);
                Assert.Equal(2, servico.Quantidade);
                Assert.Equal(120m, servico.PrecoAplicado);
                Assert.Equal(240m, servico.Subtotal);

                var peca = Assert.Single(detalhes.Pecas);
                Assert.Equal(35m, peca.PrecoAplicado);

                Assert.NotNull(detalhes.Orcamento);
                Assert.Equal("Pendente", detalhes.Orcamento.Status);
                Assert.Equal(240m, detalhes.Orcamento.ValorServicos);
                Assert.Equal(35m, detalhes.Orcamento.ValorPecas);
                Assert.Equal(275m, detalhes.Orcamento.ValorTotal);

                // Alterações no catálogo não podem mudar o preço histórico da OS.
                Assert.True(await app.Servicos.UpdateAsync(
                    cenario.ServicoId, "Alinhamento", "Serviço atualizado", 500m));

                Assert.True(await app.Pecas.UpdateAsync(
                    cenario.PecaId, "Filtro de óleo", 90m));
            }

            // Etapa 3: aprovar o orçamento e reservar a peça.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                Assert.True(await app.Aprovar.ExecuteAsync(cenario.OsId));
            }

            // Etapa 4: consultar o resultado da aprovação.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                var os = await app.Ordens.GetDetailsByIdAsync(cenario.OsId);
                var peca = await app.Pecas.GetByIdAsync(cenario.PecaId);

                Assert.NotNull(os);
                Assert.NotNull(peca);

                Assert.Equal("EmExecucao", os.Status);
                Assert.Equal("Aprovado", os.Orcamento!.Status);
                Assert.NotNull(os.DataInicioExecucao);
                Assert.Equal(275m, os.Orcamento.ValorTotal);

                Assert.Equal("Reservada", peca.Status);
                Assert.Equal(cenario.OsId, peca.OrdemServicoId);
            }

            // Etapa 5: finalizar a OS, consumindo a peça.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                Assert.True(await app.Finalizar.ExecuteAsync(cenario.OsId));
            }

            // Etapa 6: entregar a OS.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                Assert.True(await app.Ordens.Entregar(cenario.OsId));
            }

            // Etapa 7: verificar o estado final em novo contexto.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                var os = await app.Ordens.GetDetailsByIdAsync(cenario.OsId);
                var peca = await app.Pecas.GetByIdAsync(cenario.PecaId);
                var resumo = await app.Ordens.GetByIdAsync(cenario.OsId);
                var relatorio = await app.Ordens.GetTempoMedioExecucaoAsync();

                Assert.NotNull(os);
                Assert.NotNull(peca);
                Assert.NotNull(resumo);

                Assert.Equal("Entregue", os.Status);
                Assert.Equal("Entregue", resumo.Status);
                Assert.Equal("Aprovado", resumo.StatusOrcamento);

                Assert.Equal("Consumida", peca.Status);
                Assert.Equal(cenario.OsId, peca.OrdemServicoId);

                Assert.NotNull(os.DataInicioExecucao);
                Assert.NotNull(os.DataFinalizacao);
                Assert.True(os.DataFinalizacao >= os.DataInicioExecucao);

                Assert.Equal(275m, os.Orcamento!.ValorTotal);
                Assert.Single(os.Servicos);
                Assert.Single(os.Pecas);

                Assert.True(relatorio.QuantidadeOrdens >= 1);
                Assert.NotNull(relatorio.TempoMedio);
                Assert.True(relatorio.TempoMedio >= TimeSpan.Zero);
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }


    [Fact]
    public async Task DeveImpedirExecucaoDeOrcamentoRejeitado()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                Assert.True(await app.Ordens.StartDiagnosisAsync(cenario.OsId));
                Assert.True(await app.Ordens.AddServiceAsync(cenario.OsId, cenario.ServicoId, 1));
                Assert.True(await app.Ordens.AddPecaAsync(cenario.OsId, cenario.PecaId));
                Assert.True(await app.Ordens.GenerateOrcamentoAsync(cenario.OsId));
            }

            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                Assert.True(await app.Ordens.RejeitarOrcamentoAsync(cenario.OsId));
            }

            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Aprovar.ExecuteAsync(cenario.OsId));

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Finalizar.ExecuteAsync(cenario.OsId));
            }

            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                var os = await app.Ordens.GetDetailsByIdAsync(cenario.OsId);
                var peca = await app.Pecas.GetByIdAsync(cenario.PecaId);

                Assert.NotNull(os);
                Assert.NotNull(peca);

                Assert.Equal("AguardandoAprovacao", os.Status);
                Assert.Equal("Rejeitado", os.Orcamento!.Status);
                Assert.Null(os.DataInicioExecucao);
                Assert.Null(os.DataFinalizacao);

                Assert.Equal("Disponivel", peca.Status);
                Assert.Null(peca.OrdemServicoId);
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }


    [Fact]
    public async Task DeveImpedirCriacaoDeOsComVeiculoDeOutroCliente()
    {
        var cenario = await CriarCenarioAsync();
        var outroClienteId = Guid.Empty;

        try
        {
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);
                var outroCliente = SqlServerTestDatabase.NovoCliente();

                outroClienteId = await app.Clientes.CreateAsync(
                    "Outro cliente", outroCliente.Documento.Valor);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Ordens.CreateAsync(outroClienteId, cenario.VeiculoId));
            }

            await using (var context = database.CreateContext())
            {
                var existeOsInvalida = await context.OrdensServico
                    .AnyAsync(os => os.ClienteId == outroClienteId);

                Assert.False(existeOsInvalida);
            }
        }
        finally
        {
            if (outroClienteId != Guid.Empty)
                await database.CleanupAsync(clienteId: outroClienteId);

            await LimparCenarioAsync(cenario);
        }
    }


    [Fact]
    public async Task DeveImpedirExclusaoDeCadastrosVinculadosAOs()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                Assert.True(await app.Ordens.StartDiagnosisAsync(cenario.OsId));
                Assert.True(await app.Ordens.AddServiceAsync(cenario.OsId, cenario.ServicoId, 1));
                Assert.True(await app.Ordens.AddPecaAsync(cenario.OsId, cenario.PecaId));
            }

            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Clientes.DeleteAsync(cenario.ClienteId));

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Veiculos.DeleteAsync(cenario.VeiculoId));

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Servicos.DeleteAsync(cenario.ServicoId));

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => app.Pecas.DeleteAsync(cenario.PecaId));
            }

            // As tentativas de exclusão não podem apagar os cadastros.
            await using (var context = database.CreateContext())
            {
                var app = CriarAplicacao(context);

                Assert.NotNull(await app.Clientes.GetByIdAsync(cenario.ClienteId));
                Assert.NotNull(await app.Veiculos.GetByIdAsync(cenario.VeiculoId));
                Assert.NotNull(await app.Servicos.GetByIdAsync(cenario.ServicoId));
                Assert.NotNull(await app.Pecas.GetByIdAsync(cenario.PecaId));
                Assert.NotNull(await app.Ordens.GetByIdAsync(cenario.OsId));
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }


    private async Task<Cenario> CriarCenarioAsync()
    {
        await using var context = database.CreateContext();
        var app = CriarAplicacao(context);

        var clienteTeste = SqlServerTestDatabase.NovoCliente();
        var veiculoTeste = SqlServerTestDatabase.NovoVeiculo(clienteTeste.Id);

        var clienteId = await app.Clientes.CreateAsync(
            clienteTeste.Nome,
            clienteTeste.Documento.Valor);

        var veiculoId = await app.Veiculos.CreateAsync(
            clienteId,
            veiculoTeste.Placa.Valor,
            "Ford",
            "Ka",
            2020);

        var servicoId = await app.Servicos.CreateAsync(
            "Alinhamento",
            "Alinhamento das rodas",
            120m);

        var pecaId = await app.Pecas.CreateAsync(
            "Filtro de óleo",
            35m);

        var osId = await app.Ordens.CreateAsync(clienteId, veiculoId);

        return new Cenario(clienteId, veiculoId, servicoId, pecaId, osId);
    }

    private Task LimparCenarioAsync(Cenario cenario)
    {
        return database.CleanupAsync(
            clienteId: cenario.ClienteId,
            veiculoId: cenario.VeiculoId,
            servicoId: cenario.ServicoId,
            pecaId: cenario.PecaId,
            ordemServicoId: cenario.OsId);
    }

    private static Aplicacao CriarAplicacao(OficinaDbContext context)
    {
        var clienteRepository = new ClienteRepository(context);
        var veiculoRepository = new VeiculoRepository(context);
        var servicoRepository = new ServicoRepository(context);
        var pecaRepository = new PecaInsumoRepository(context);
        var osRepository = new OrdemServicoRepository(context);

        var transactionManager = new EfTransactionManager(context);

        return new Aplicacao(
            new ClienteApplicationService(clienteRepository),
            new VeiculoApplicationService(veiculoRepository, clienteRepository),
            new ServicoApplicationService(servicoRepository),
            new PecaInsumoApplicationService(pecaRepository),
            new OrdemServicoApplicationService(
                osRepository,
                clienteRepository,
                veiculoRepository,
                servicoRepository,
                pecaRepository),
            new AprovarOrcamentoUseCase(osRepository, pecaRepository, transactionManager),
            new FinalizarOrdemServicoUseCase(osRepository, pecaRepository, transactionManager));
    }

    private sealed record Cenario(
        Guid ClienteId,
        Guid VeiculoId,
        Guid ServicoId,
        Guid PecaId,
        Guid OsId);

    private sealed record Aplicacao(
        ClienteApplicationService Clientes,
        VeiculoApplicationService Veiculos,
        ServicoApplicationService Servicos,
        PecaInsumoApplicationService Pecas,
        OrdemServicoApplicationService Ordens,
        AprovarOrcamentoUseCase Aprovar,
        FinalizarOrdemServicoUseCase Finalizar);



}
