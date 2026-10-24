
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[Collection("SqlServerIntegration")]
public class PecaInsumoRepositoryIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DevePersistirAtualizarEExcluirPeca()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);

        try
        {
            await using (var context = database.CreateContext())
            {
                await new PecaInsumoRepository(context).CreateAsync(peca);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new PecaInsumoRepository(context);
                var encontrada = await repository.GetByIdAsync(peca.Id);

                Assert.NotNull(encontrada);
                Assert.Equal(StatusPecaInsumo.Disponivel, encontrada.Status);
                Assert.False(await repository.HasDependenciesAsync(peca.Id));
                Assert.Contains(await repository.ListAsync(), p => p.Id == peca.Id);

                encontrada.Update("Filtro de ar", 50m);
                await repository.UpdateAsync(encontrada);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new PecaInsumoRepository(context);
                var atualizada = await repository.GetByIdAsync(peca.Id);

                Assert.NotNull(atualizada);
                Assert.Equal("Filtro de ar", atualizada.Nome);
                Assert.Equal(50m, atualizada.Preco);

                await repository.DeleteAsync(atualizada);
            }

            await using (var context = database.CreateContext())
            {
                Assert.Null(await new PecaInsumoRepository(context).GetByIdAsync(peca.Id));
            }
        }
        finally
        {
            await database.CleanupAsync(pecaId: peca.Id);
        }
    }

    [Fact]
    public async Task DevePersistirReservaEConsumoDaPeca()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AdicionarPeca(peca.Id, peca.Preco);
        os.GerarOrcamento();

        try
        {
            await using (var context = database.CreateContext())
            {
                context.Clientes.Add(cliente);
                context.Veiculos.Add(veiculo);
                context.PecasInsumos.Add(peca);
                context.OrdensServico.Add(os);
                await context.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                var repository = new PecaInsumoRepository(context);
                var encontrada = await repository.GetByIdAsync(peca.Id);

                Assert.NotNull(encontrada);
                Assert.True(await repository.HasDependenciesAsync(peca.Id));

                encontrada.Reservar(os.Id);
                await repository.UpdateAsync(encontrada);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new PecaInsumoRepository(context);
                var reservada = await repository.GetByIdAsync(peca.Id);

                Assert.NotNull(reservada);
                Assert.Equal(StatusPecaInsumo.Reservada, reservada.Status);
                Assert.Equal(os.Id, reservada.OrdemServicoId);

                reservada.Consumir(os.Id);
                await repository.UpdateAsync(reservada);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new PecaInsumoRepository(context);
                var consumida = await repository.GetByIdAsync(peca.Id);

                Assert.NotNull(consumida);
                Assert.Equal(StatusPecaInsumo.Consumida, consumida.Status);
                Assert.Equal(os.Id, consumida.OrdemServicoId);
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id, pecaId: peca.Id, ordemServicoId: os.Id);
        }
    }
}
