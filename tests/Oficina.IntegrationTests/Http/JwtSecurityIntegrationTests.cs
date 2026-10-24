
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Oficina.IntegrationTests.Persistence;
using Xunit;

namespace Oficina.IntegrationTests.Http;

[Collection("SqlServerIntegration")]
public sealed class JwtSecurityIntegrationTests(
    SqlServerTestDatabase database)
{
    [Theory]
    [InlineData("expirado")]
    [InlineData("assinatura-incorreta")]
    [InlineData("emissor-incorreto")]
    [InlineData("audiencia-incorreta")]
    [InlineData("ainda-nao-valido")]
    public async Task DeveRejeitarTokenJwtInvalido(string cenario)
    {
        using var factory = new OficinaApiFactory(database);

        var agora = DateTime.UtcNow;

        using var client = cenario switch
        {
            "expirado" => factory.CriarClienteComTokenDeTeste(
                inicioValidadeUtc: agora.AddHours(-2),
                expiracaoUtc: agora.AddHours(-1)),

            "assinatura-incorreta" => factory.CriarClienteComTokenDeTeste(
                assinaturaIncorreta: true),

            "emissor-incorreto" => factory.CriarClienteComTokenDeTeste(
                emissor: "Emissor.NaoConfiavel"),

            "audiencia-incorreta" => factory.CriarClienteComTokenDeTeste(
                audiencia: "Outra.Aplicacao"),

            "ainda-nao-valido" => factory.CriarClienteComTokenDeTeste(
                inicioValidadeUtc: agora.AddMinutes(5),
                expiracaoUtc: agora.AddMinutes(10)),

            _ => throw new ArgumentOutOfRangeException(nameof(cenario))
        };

        var response = await client.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task DeveRejeitarTokenMalformado()
    {
        using var factory = new OficinaApiFactory(database);
        using var client = factory.CriarClienteSemToken();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "isto-nao-e-um-jwt");

        var response = await client.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task DeveNegarAcessoAdministrativoSemRole()
    {
        using var factory = new OficinaApiFactory(database);

        using var client = factory.CriarClienteComTokenDeTeste(
            role: null);

        var response = await client.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task DeveNegarAcompanhamentoComClienteIdInvalido()
    {
        using var factory = new OficinaApiFactory(database);

        using var client = factory.CriarClienteComTokenDeTeste(
            role: "Cliente",
            clienteId: "nao-e-um-guid");

        var response = await client.GetAsync(
            $"/api/acompanhamento/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ClienteNaoPodeCadastrarServico()
    {
        var nomeServico = $"Servico-Negado-{Guid.NewGuid():N}";

        try
        {
            using var factory = new OficinaApiFactory(database);

            using var client = factory.CriarClienteAutenticado(
                "Cliente",
                Guid.NewGuid());

            var response = await client.PostAsJsonAsync(
                "/api/servicos",
                new
                {
                    nome = nomeServico,
                    descricao = "Tentativa sem permissão administrativa",
                    preco = 50m
                });

            Assert.Equal(
                HttpStatusCode.Forbidden,
                response.StatusCode);

            // A requisição negada não pode gerar registros.
            await using var context = database.CreateContext();

            var existeServico = await context.Servicos
                .AnyAsync(s => s.Nome == nomeServico);

            Assert.False(existeServico);
        }
        finally
        {
            // Limpa somente os dados exclusivos deste teste,
            // mesmo se ocorrer uma falha na asserção.
            await using var context = database.CreateContext();

            await context.Servicos
                .Where(s => s.Nome == nomeServico)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task AdminComTokenValidoPodeConsultarClientes()
    {
        using var factory = new OficinaApiFactory(database);

        using var client = factory.CriarClienteAutenticado(
            "Admin");

        var response = await client.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
}
