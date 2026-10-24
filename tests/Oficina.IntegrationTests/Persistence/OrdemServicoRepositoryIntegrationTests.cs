
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[Collection("SqlServerIntegration")]
public class OrdemServicoRepositoryIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DevePersistirEReconstruirAgregadoCompleto()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(cliente.Id);
        var servico = new Servico("Alinhamento", "Alinhamento das rodas", 120m);
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = new OrdemServico(cliente.Id, veiculo.Id);

        os.CreateDiagnostic();
        os.AddDiagnostic("Desgaste nas peças.");
        os.AdicionarServico(servico.Id, 2, servico.Preco);
        os.AdicionarPeca(peca.Id, peca.Preco);
        os.GerarOrcamento();

        try
        {
            await using (var context = database.CreateContext())
            {
                context.Clientes.Add(cliente);
                context.Veiculos.Add(veiculo);
                context.Servicos.Add(servico);
                context.PecasInsumos.Add(peca);
                context.OrdensServico.Add(os);
                await context.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                var repository = new OrdemServicoRepository(context);
                var encontrada = await repository.GetByIdAsync(os.Id);

                Assert.NotNull(encontrada);
                Assert.Equal(StatusOS.AguardandoAprovacao, encontrada.Status);

                Assert.NotNull(encontrada.Diagnostico);
                Assert.Equal("Desgaste nas peças.", encontrada.Diagnostico.Descricao);

                var itemServico = Assert.Single(encontrada.ItensServico);
                Assert.Equal(servico.Id, itemServico.ServicoId);
                Assert.Equal(2, itemServico.Quantidade);
                Assert.Equal(240m, itemServico.CalcularSubtotal());

                var itemPeca = Assert.Single(encontrada.ItensPeca);
                Assert.Equal(peca.Id, itemPeca.PecaInsumoId);
                Assert.Equal(35m, itemPeca.PrecoAplicado);

                Assert.NotNull(encontrada.Orcamento);
                Assert.Equal(275m, encontrada.Orcamento.ValorTotal);
                Assert.Equal(StatusOrcamento.Pendente, encontrada.Orcamento.Status);

                encontrada.RejeitarOrcamento();
                await repository.UpdateAsync(encontrada);
            }

            await using (var context = database.CreateContext())
            {
                var repository = new OrdemServicoRepository(context);
                var encontrada = await repository.GetByIdAsync(os.Id);

                Assert.NotNull(encontrada);
                Assert.Equal(StatusOrcamento.Rejeitado, encontrada.Orcamento!.Status);
                Assert.Single(encontrada.ItensServico);
                Assert.Single(encontrada.ItensPeca);

                var lista = await repository.ListAsync();
                var listada = Assert.Single(lista, o => o.Id == os.Id);

                Assert.NotNull(listada.Diagnostico);
                Assert.NotNull(listada.Orcamento);
                Assert.Single(listada.ItensServico);
                Assert.Single(listada.ItensPeca);
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id, servicoId: servico.Id, pecaId: peca.Id, ordemServicoId: os.Id);
        }
    }

    [Fact]
    public async Task DeveConsultarOsRecemCriada()
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
                await context.SaveChangesAsync();

                await new OrdemServicoRepository(context).CreateAsync(os);
            }

            await using (var context = database.CreateContext())
            {
                var encontrada = await new OrdemServicoRepository(context).GetByIdAsync(os.Id);

                Assert.NotNull(encontrada);
                Assert.Equal(StatusOS.Recebida, encontrada.Status);
                Assert.Null(encontrada.Diagnostico);
                Assert.Null(encontrada.Orcamento);
                Assert.Empty(encontrada.ItensServico);
                Assert.Empty(encontrada.ItensPeca);
            }
        }
        finally
        {
            await database.CleanupAsync(clienteId: cliente.Id, veiculoId: veiculo.Id, ordemServicoId: os.Id);
        }
    }
}
