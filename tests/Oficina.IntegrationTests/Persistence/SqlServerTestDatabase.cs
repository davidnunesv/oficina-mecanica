
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;
using Oficina.Infrastructure.Persistence;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

[CollectionDefinition("SqlServerIntegration", DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection : ICollectionFixture<SqlServerTestDatabase>
{
}

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    public OficinaDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("OFICINA_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Configure OFICINA_TEST_CONNECTION_STRING.");

        var builder = new SqlConnectionStringBuilder(connectionString);

        if (!string.Equals(builder.InitialCatalog, "OficinaMecanica_IntegrationTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Utilize exclusivamente o banco OficinaMecanica_IntegrationTests.");

        if (!string.Equals(builder.DataSource, "127.0.0.1,14333", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(builder.DataSource, "localhost,14333", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Utilize o SQL Server Docker local na porta 14333.");

        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new OficinaDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static Cliente NovoCliente()
    {
        var baseCpf = Random.Shared.Next(100_000_000, 999_999_999).ToString("D9");
        var primeiro = CalcularDigito(baseCpf, 10);
        var segundo = CalcularDigito(baseCpf + primeiro, 11);

        return new Cliente("Cliente de integração", new Documento(baseCpf + primeiro + segundo));
    }

    public static Veiculo NovoVeiculo(Guid clienteId)
    {
        var letras = new string(Enumerable.Range(0, 3).Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray());
        var placa = new Placa($"{letras}{Random.Shared.Next(0, 10000):D4}");

        return new Veiculo(clienteId, placa, "Ford", "Ka", 2020);
    }

    private static int CalcularDigito(string numeros, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < numeros.Length; i++)
            soma += (numeros[i] - '0') * (pesoInicial - i);

        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }

    public async Task CleanupAsync(Guid? clienteId = null, Guid? veiculoId = null, Guid? servicoId = null, Guid? pecaId = null, Guid? ordemServicoId = null)
    {
        await using var context = CreateContext();

        if (ordemServicoId.HasValue)
            await context.Set<ItemPecaOS>().Where(i => EF.Property<Guid>(i, "OrdemServicoId") == ordemServicoId.Value).ExecuteDeleteAsync();

        if (pecaId.HasValue)
            await context.PecasInsumos.Where(p => p.Id == pecaId.Value).ExecuteDeleteAsync();

        if (ordemServicoId.HasValue)
            await context.OrdensServico.Where(os => os.Id == ordemServicoId.Value).ExecuteDeleteAsync();

        if (servicoId.HasValue)
            await context.Servicos.Where(s => s.Id == servicoId.Value).ExecuteDeleteAsync();

        if (veiculoId.HasValue)
            await context.Veiculos.Where(v => v.Id == veiculoId.Value).ExecuteDeleteAsync();

        if (clienteId.HasValue)
            await context.Clientes.Where(c => c.Id == clienteId.Value).ExecuteDeleteAsync();
    }
}
