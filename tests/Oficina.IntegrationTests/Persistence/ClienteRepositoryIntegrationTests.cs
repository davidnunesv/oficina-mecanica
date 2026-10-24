
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

public class ClienteRepositoryIntegrationTests
{
    [Fact]
    public async Task DevePersistirConsultarAtualizarEExcluirCliente()
    {
        var documentoOriginal = GerarCpfValido();
        var documentoAtualizado = GerarCpfValido();
        var cliente = new Cliente("David Nunes", documentoOriginal);

        await using (var context = CriarContexto())
        {
            await context.Database.MigrateAsync();
        }

        try
        {
            // Primeiro contexto: inserir no banco.
            await using (var context = CriarContexto())
            {
                var repository = new ClienteRepository(context);

                await repository.CreateAsync(cliente);
            }

            // Segundo contexto: consultar e atualizar.
            await using (var context = CriarContexto())
            {
                var repository = new ClienteRepository(context);

                var encontrado = await repository.GetByIdAsync(cliente.Id);

                Assert.NotNull(encontrado);
                Assert.Equal(cliente.Id, encontrado.Id);
                Assert.Equal("David Nunes", encontrado.Nome);
                Assert.Equal(documentoOriginal, encontrado.Documento);

                Assert.True(await repository.HasDocumentoAsync(documentoOriginal));
                Assert.False(await repository.HasDependenciesAsync(cliente.Id));
                Assert.Contains(await repository.ListAsync(), c => c.Id == cliente.Id);

                encontrado.Update("João Silva", documentoAtualizado);

                await repository.UpdateAsync(encontrado);
            }

            // Terceiro contexto: confirmar a atualização e excluir.
            await using (var context = CriarContexto())
            {
                var repository = new ClienteRepository(context);

                var atualizado = await repository.GetByIdAsync(cliente.Id);

                Assert.NotNull(atualizado);
                Assert.Equal("João Silva", atualizado.Nome);
                Assert.Equal(documentoAtualizado, atualizado.Documento);
                Assert.True(await repository.HasDocumentoAsync(documentoAtualizado));
                Assert.False(await repository.HasDocumentoAsync(documentoOriginal));

                await repository.DeleteAsync(atualizado);
            }

            // Quarto contexto: confirmar a exclusão.
            await using (var context = CriarContexto())
            {
                var repository = new ClienteRepository(context);

                Assert.Null(await repository.GetByIdAsync(cliente.Id));
            }
        }
        finally
        {
            // Limpeza caso alguma asserção falhe.
            await using var context = CriarContexto();
            await context.Clientes.Where(c => c.Id == cliente.Id).ExecuteDeleteAsync();
        }
    }

    private static OficinaDbContext CriarContexto()
    {
        var connectionString = Environment.GetEnvironmentVariable("OFICINA_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Configure a variável OFICINA_TEST_CONNECTION_STRING antes de executar os testes.");

        var builder = new SqlConnectionStringBuilder(connectionString);

        if (!string.Equals(builder.InitialCatalog, "OficinaMecanica_IntegrationTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Os testes só podem utilizar o banco OficinaMecanica_IntegrationTests.");

        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new OficinaDbContext(options);
    }

    private static Documento GerarCpfValido()
    {
        var baseCpf = Random.Shared.Next(100_000_000, 999_999_999).ToString("D9");

        var primeiroDigito = CalcularDigito(baseCpf, 10);
        var segundoDigito = CalcularDigito(baseCpf + primeiroDigito, 11);

        return new Documento(baseCpf + primeiroDigito + segundoDigito);
    }

    private static int CalcularDigito(string numeros, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < numeros.Length; i++)
            soma += (numeros[i] - '0') * (pesoInicial - i);

        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }
}
