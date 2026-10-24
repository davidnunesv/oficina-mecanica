
using Oficina.Domain.Entities;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[Collection("SqlServerIntegration")]
public class ServicoRepositoryIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DevePersistirAtualizarEExcluirServico()
    {
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);

        try
        {
            await using (var context = database.CreateContext())
            {
                await new ServicoRepository(context).CreateAsync(servico);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new ServicoRepository(context);
                var encontrado = await repository.GetByIdAsync(servico.Id);

                Assert.NotNull(encontrado);
                Assert.Equal(100m, encontrado.Preco);
                Assert.False(await repository.HasDependenciesAsync(servico.Id));
                Assert.Contains(await repository.ListAsync(), s => s.Id == servico.Id);

                encontrado.Update("Balanceamento", "Balanceamento das rodas", 120m);
                await repository.UpdateAsync(encontrado);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new ServicoRepository(context);
                var atualizado = await repository.GetByIdAsync(servico.Id);

                Assert.NotNull(atualizado);
                Assert.Equal("Balanceamento", atualizado.Nome);
                Assert.Equal(120m, atualizado.Preco);

                await repository.DeleteAsync(atualizado);
            }

            await using (var context = database.CreateContext())
            {
                Assert.Null(await new ServicoRepository(context).GetByIdAsync(servico.Id));
            }
        }
        finally
        {
            await database.CleanupAsync(servicoId: servico.Id);
        }
    }

    [Fact]
    public async Task DeveIdentificarServicoUtilizadoEmOs()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 100m);
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AdicionarServico(servico.Id, 1, servico.Preco);

        try
        {
            await using (var context = database.CreateContext())
            {
                context.Clientes.Add(cliente);
                context.Veiculos.Add(veiculo);
                context.Servicos.Add(servico);
                context.OrdensServico.Add(os);
                await context.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                var repository = new ServicoRepository(context);

                Assert.True(await repository.HasDependenciesAsync(servico.Id));
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id, servicoId: servico.Id, ordemServicoId: os.Id);
        }
    }
}
