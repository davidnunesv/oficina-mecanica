
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Oficina.Infrastructure.Persistence;
using Oficina.IntegrationTests.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using ApiProgram = global::Program;

namespace Oficina.IntegrationTests.Http;

[Collection("SqlServerIntegration")]
public class ClientesHttpIntegrationTests(SqlServerTestDatabase database)
{
    [Fact]
    public async Task DeveCriarConsultarEImpedirClienteDuplicadoViaHttp()
    {
        var cliente = SqlServerTestDatabase.NovoCliente();
        var id = Guid.Empty;

        try
        {
            using var factory = CriarFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

            // Criar cliente via API.
            var response = await client.PostAsJsonAsync("/api/clientes", new
            {
                nome = cliente.Nome,
                documento = cliente.Documento.Valor
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            using (var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                id = json.RootElement.GetProperty("id").GetGuid();
            }

            Assert.NotEqual(Guid.Empty, id);

            // Consultar o cliente pelo endpoint retornado.
            var consulta = await client.GetAsync($"/api/clientes/{id}");

            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

            using (var json = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync()))
            {
                Assert.Equal(id, json.RootElement.GetProperty("id").GetGuid());
                Assert.Equal(cliente.Documento.Valor, json.RootElement.GetProperty("documento").GetString());
            }

            // Tentar cadastrar o mesmo documento.
            var duplicado = await client.PostAsJsonAsync("/api/clientes", new
            {
                nome = "Outro nome",
                documento = cliente.Documento.Valor
            });

            Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        }
        finally
        {
            await using var context = database.CreateContext();

            var clienteId = await context.Clientes
                .AsNoTracking()
                .Where(c => c.Documento == cliente.Documento)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (clienteId != Guid.Empty)
                await database.CleanupAsync(clienteId: clienteId);
        }
    }

    [Fact]
    public async Task DeveRetornar400ParaRequestInvalidoE404ParaRecursoInexistente()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var invalido = await client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "",
            documento = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var inexistente = await client.GetAsync($"/api/clientes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }


    private WebApplicationFactory<ApiProgram> CriarFactory()
    {
        Assert.Equal("Oficina.Api", typeof(ApiProgram).Assembly.GetName().Name);

        // Reaproveita a validação do nome do banco e do servidor.
        using var validationContext = database.CreateContext();

        var connectionString = Environment.GetEnvironmentVariable("OFICINA_TEST_CONNECTION_STRING")!;
        var factory = new TestApiFactory(connectionString);

        // Verifica a conexão efetiva resolvida pela DI antes de enviar HTTP.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();

        if (context.Database.GetDbConnection().Database != "OficinaMecanica_IntegrationTests")
        {
            factory.Dispose();
            throw new InvalidOperationException("A API de testes não está apontando para o banco de integração.");
        }

        return factory;
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<ApiProgram>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                });
            });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            base.ConfigureWebHost(builder);
        }
    }

}
