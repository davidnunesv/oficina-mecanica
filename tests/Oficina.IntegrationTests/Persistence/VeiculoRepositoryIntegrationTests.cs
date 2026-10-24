
using Oficina.Domain.Entities;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[Collection("SqlServerIntegration")]
public class VeiculoRepositoryIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DevePersistirAtualizarEExcluirVeiculo()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var novaPlaca = SqlServerTestDatabase.NovoVeiculo(cliente.Id).Placa;

        try
        {
            await using (var context = database.CreateContext())
            {
                context.Clientes.Add(cliente);
                await context.SaveChangesAsync();

                await new VeiculoRepository(context).CreateAsync(veiculo);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new VeiculoRepository(context);
                var encontrado = await repository.GetByIdAsync(veiculo.Id);

                Assert.NotNull(encontrado);
                Assert.Equal(cliente.Id, encontrado.ClienteId);
                Assert.Equal(veiculo.Placa, encontrado.Placa);
                Assert.True(await repository.HasPlacaAsync(veiculo.Placa));
                Assert.False(await repository.HasDependenciesAsync(veiculo.Id));
                Assert.Contains(await repository.ListAsync(), v => v.Id == veiculo.Id);

                encontrado.Update(novaPlaca, "Honda", "Civic", 2022);
                await repository.UpdateAsync(encontrado);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new VeiculoRepository(context);
                var atualizado = await repository.GetByIdAsync(veiculo.Id);

                Assert.NotNull(atualizado);
                Assert.Equal("Honda", atualizado.Marca);
                Assert.Equal("Civic", atualizado.Modelo);
                Assert.Equal(2022, atualizado.Ano);
                Assert.Equal(novaPlaca, atualizado.Placa);

                await repository.DeleteAsync(atualizado);
            }

            await using (var context = database.CreateContext())
            {
                Assert.Null(await new VeiculoRepository(context).GetByIdAsync(veiculo.Id));
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id);
        }
    }

    [Fact]
    public async Task DeveIdentificarDependenciaComOrdemServico()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        try
        {
            await using (var context = database.CreateContext())
            {
                context.Clientes.Add(cliente);
                context.Veiculos.Add(veiculo);
                context.OrdensServico.Add(os);
                await context.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                var repository = new VeiculoRepository(context);

                Assert.True(await repository.HasDependenciesAsync(veiculo.Id));
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id, ordemServicoId: os.Id);
        }
    }
}
