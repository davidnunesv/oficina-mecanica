
using Microsoft.EntityFrameworkCore;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[Collection("SqlServerIntegration")]
public class EfTransactionManagerIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DeveConfirmarAprovacaoEFinalizacaoDaOs()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            // Aprovação usando uma transação real.
            await using (var context = database.CreateContext())
            {
                var useCase = new AprovarOrcamentoUseCase(
                    new OrdemServicoRepository(context),
                    new PecaInsumoRepository(context),
                    new EfTransactionManager(context));

                Assert.True(await useCase.ExecuteAsync(cenario.OrdemServicoId, cenario.ClienteId));
            }

            // Confirmar que todas as mudanças foram persistidas.
            await using (var context = database.CreateContext())
            {
                var os = await new OrdemServicoRepository(context).GetByIdAsync(cenario.OrdemServicoId);
                var peca = await new PecaInsumoRepository(context).GetByIdAsync(cenario.PecaId);

                Assert.NotNull(os);
                Assert.NotNull(peca);

                Assert.Equal(StatusOS.EmExecucao, os.Status);
                Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
                Assert.NotNull(os.DataInicioExecucao);
                Assert.Equal(StatusPecaInsumo.Reservada, peca.Status);
                Assert.Equal(os.Id, peca.OrdemServicoId);
            }

            // Finalização usando outra transação real.
            await using (var context = database.CreateContext())
            {
                var useCase = new FinalizarOrdemServicoUseCase(
                    new OrdemServicoRepository(context),
                    new PecaInsumoRepository(context),
                    new EfTransactionManager(context));

                Assert.True(await useCase.ExecuteAsync(cenario.OrdemServicoId));
            }

            // Verificar o resultado final em um contexto novo.
            await using (var context = database.CreateContext())
            {
                var os = await new OrdemServicoRepository(context).GetByIdAsync(cenario.OrdemServicoId);
                var peca = await new PecaInsumoRepository(context).GetByIdAsync(cenario.PecaId);

                Assert.NotNull(os);
                Assert.NotNull(peca);

                Assert.Equal(StatusOS.Finalizada, os.Status);
                Assert.NotNull(os.DataFinalizacao);
                Assert.Equal(StatusPecaInsumo.Consumida, peca.Status);
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }

    [Fact]
    public async Task DeveExecutarRollbackMesmoAposMultiplosSaveChanges()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            await using (var context = database.CreateContext())
            {
                var ordemRepository = new OrdemServicoRepository(context);
                var pecaRepository = new PecaInsumoRepository(context);
                var transactionManager = new EfTransactionManager(context);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await transactionManager.ExecuteInTransactionAsync<bool>(async ct =>
                    {
                        var os = await ordemRepository.GetByIdAsync(cenario.OrdemServicoId, ct)
                            ?? throw new InvalidOperationException("OS não encontrada.");

                        var peca = await pecaRepository.GetByIdAsync(cenario.PecaId, ct)
                            ?? throw new InvalidOperationException("Peça não encontrada.");

                        peca.Reservar(os.Id);
                        await pecaRepository.UpdateAsync(peca, ct);

                        os.AprovarOrcamento();
                        os.IniciarExecucao();
                        await ordemRepository.UpdateAsync(os, ct);

                        throw new InvalidOperationException("Falha simulada após as gravações.");
                    });
                });

                Assert.Equal("Falha simulada após as gravações.", exception.Message);
            }

            // Outro DbContext confirma que nada foi efetivado.
            await using (var context = database.CreateContext())
            {
                var os = await new OrdemServicoRepository(context).GetByIdAsync(cenario.OrdemServicoId);
                var peca = await new PecaInsumoRepository(context).GetByIdAsync(cenario.PecaId);

                Assert.NotNull(os);
                Assert.NotNull(peca);

                Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
                Assert.Equal(StatusOrcamento.Pendente, os.Orcamento!.Status);
                Assert.Null(os.DataInicioExecucao);
                Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
                Assert.Null(peca.OrdemServicoId);
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }

    [Fact]
    public async Task DeveDetectarDuasReservasConcorrentesDaMesmaPeca()
    {
        var cenario = await CriarCenarioAsync();
        var segundaOs = new OrdemServico(cenario.ClienteId, cenario.VeiculoId);

        try
        {
            await using (var context = database.CreateContext())
            {
                context.OrdensServico.Add(segundaOs);
                await context.SaveChangesAsync();
            }

            await using (var contextA = database.CreateContext())
            await using (var contextB = database.CreateContext())
            {
                var repositoryA = new PecaInsumoRepository(contextA);
                var repositoryB = new PecaInsumoRepository(contextB);

                // Duas requisições leem a peça enquanto disponível.
                var pecaA = await repositoryA.GetByIdAsync(cenario.PecaId);
                var pecaB = await repositoryB.GetByIdAsync(cenario.PecaId);

                Assert.NotNull(pecaA);
                Assert.NotNull(pecaB);
                Assert.Equal(StatusPecaInsumo.Disponivel, pecaA.Status);
                Assert.Equal(StatusPecaInsumo.Disponivel, pecaB.Status);

                // A primeira requisição reserva com sucesso.
                pecaA.Reservar(cenario.OrdemServicoId);
                await repositoryA.UpdateAsync(pecaA);

                // A segunda tenta persistir uma versão desatualizada.
                pecaB.Reservar(segundaOs.Id);

                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                    () => repositoryB.UpdateAsync(pecaB));
            }

            await using (var context = database.CreateContext())
            {
                var peca = await new PecaInsumoRepository(context).GetByIdAsync(cenario.PecaId);

                Assert.NotNull(peca);
                Assert.Equal(StatusPecaInsumo.Reservada, peca.Status);
                Assert.Equal(cenario.OrdemServicoId, peca.OrdemServicoId);
            }
        }
        finally
        {
            await database.CleanupAsync(ordemServicoId: segundaOs.Id);
            await LimparCenarioAsync(cenario);
        }
    }

    [Fact]
    public async Task DeveDetectarAprovacaoDeOrcamentoRejeitadoPorOutraRequisicao()
    {
        var cenario = await CriarCenarioAsync();

        try
        {
            await using (var contextA = database.CreateContext())
            await using (var contextB = database.CreateContext())
            {
                var repositoryA = new OrdemServicoRepository(contextA);
                var repositoryB = new OrdemServicoRepository(contextB);

                var osA = await repositoryA.GetByIdAsync(cenario.OrdemServicoId);
                var osB = await repositoryB.GetByIdAsync(cenario.OrdemServicoId);

                Assert.NotNull(osA);
                Assert.NotNull(osB);

                osA.RejeitarOrcamento();
                await repositoryA.UpdateAsync(osA);

                osB.AprovarOrcamento();
                osB.IniciarExecucao();

                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                    () => repositoryB.UpdateAsync(osB));
            }

            await using (var context = database.CreateContext())
            {
                var os = await new OrdemServicoRepository(context).GetByIdAsync(cenario.OrdemServicoId);

                Assert.NotNull(os);
                Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
                Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
                Assert.Null(os.DataInicioExecucao);
            }
        }
        finally
        {
            await LimparCenarioAsync(cenario);
        }
    }

    private async Task<Cenario> CriarCenarioAsync()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var peca = new PecaInsumo("Peça para teste transacional", 75m);
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AdicionarPeca(peca.Id, peca.Preco);
        os.GerarOrcamento();

        await using var context = database.CreateContext();

        context.Clientes.Add(cliente);
        context.Veiculos.Add(veiculo);
        context.PecasInsumos.Add(peca);
        context.OrdensServico.Add(os);

        await context.SaveChangesAsync();

        return new Cenario(cliente.Id, veiculo.Id, peca.Id, os.Id);
    }

    private Task LimparCenarioAsync(Cenario cenario)
    {
        return database.CleanupAsync(
            clienteId: cenario.ClienteId,
            veiculoId: cenario.VeiculoId,
            pecaId: cenario.PecaId,
            ordemServicoId: cenario.OrdemServicoId);
    }

    private sealed record Cenario(Guid ClienteId, Guid VeiculoId, Guid PecaId, Guid OrdemServicoId);
}
