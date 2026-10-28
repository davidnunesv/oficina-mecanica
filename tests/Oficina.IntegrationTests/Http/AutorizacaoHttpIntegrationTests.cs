
using System.Net;
using System.Net.Http.Json;
using Oficina.Application.DTOs.OrdensServico;
using Oficina.Domain.Entities;
using Oficina.IntegrationTests.Persistence;
using Xunit;

namespace Oficina.IntegrationTests.Http;

[Collection("SqlServerIntegration")]
public sealed class AutorizacaoHttpIntegrationTests(
    SqlServerTestDatabase database)
{
    [Fact]
    public async Task DeveProtegerEndpointsAdministrativos()
    {
        using var factory = new OficinaApiFactory(database);

        // Sem token: 401.
        using var anonimo = factory.CriarClienteSemToken();

        var respostaAnonima =
            await anonimo.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            respostaAnonima.StatusCode);

        // Cliente autenticado, mas sem permissão Admin: 403.
        using var cliente = factory.CriarClienteAutenticado(
            "Cliente",
            Guid.NewGuid());

        var respostaCliente =
            await cliente.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            respostaCliente.StatusCode);

        // Administrador: 200.
        using var admin = factory.CriarClienteAutenticado("Admin");

        var respostaAdmin =
            await admin.GetAsync("/api/clientes");

        Assert.Equal(
            HttpStatusCode.OK,
            respostaAdmin.StatusCode);
    }

    [Fact]
    public async Task ClienteDeveConsultarSomenteSuaPropriaOs()
    {
        var proprietario = SqlServerTestDatabase.NovoCliente();
        var outroCliente = SqlServerTestDatabase.NovoCliente();
        var veiculo = SqlServerTestDatabase.NovoVeiculo(
            proprietario.Id);

        var os = new OrdemServico(
            proprietario.Id,
            veiculo.Id);

        try
        {
            // Preparar uma OS real no banco de integração.
            await using (var context = database.CreateContext())
            {
                context.Clientes.AddRange(
                    proprietario,
                    outroCliente);

                context.Veiculos.Add(veiculo);
                context.OrdensServico.Add(os);

                await context.SaveChangesAsync();
            }

            using var factory = new OficinaApiFactory(database);

            var url = $"/api/acompanhamento/{os.Id}";

            // O proprietário consegue acompanhar.
            using (var cliente = factory.CriarClienteAutenticado(
                "Cliente", proprietario.Id))
            {
                var resposta = await cliente.GetAsync(url);

                Assert.Equal(
                    HttpStatusCode.OK,
                    resposta.StatusCode);

                var resultado = await resposta.Content.ReadFromJsonAsync<OrdemServicoDetalheDto>();

                Assert.NotNull(resultado);
                Assert.Equal(os.Id, resultado.Id);
                Assert.Equal(proprietario.Id, resultado.ClienteId);
                Assert.Equal("Recebida", resultado.Status);

                // Nesse teste, a OS acabou de ser criada.
                Assert.Null(resultado.Orcamento);
            }

            // Outro cliente não pode consultar a OS.
            using (var cliente = factory.CriarClienteAutenticado(
                "Cliente", outroCliente.Id))
            {
                var resposta = await cliente.GetAsync(url);

                Assert.Equal(
                    HttpStatusCode.NotFound,
                    resposta.StatusCode);
            }

            // Um token de cliente sem vínculo também não serve.
            using (var cliente = factory.CriarClienteAutenticado(
                "Cliente"))
            {
                var resposta = await cliente.GetAsync(url);

                Assert.Equal(
                    HttpStatusCode.Forbidden,
                    resposta.StatusCode);
            }

            // A rota de acompanhamento exige a role Cliente.
            using (var admin = factory.CriarClienteAutenticado("Admin"))
            {
                var resposta = await admin.GetAsync(url);

                Assert.Equal(
                    HttpStatusCode.Forbidden,
                    resposta.StatusCode);
            }

            // Sem autenticação, o acesso é negado.
            using (var anonimo = factory.CriarClienteSemToken())
            {
                var resposta = await anonimo.GetAsync(url);

                Assert.Equal(
                    HttpStatusCode.Unauthorized,
                    resposta.StatusCode);
            }
        }
        finally
        {
            // Excluir a OS antes de excluir veículo e proprietário.
            await database.CleanupAsync(
                clienteId: proprietario.Id,
                veiculoId: veiculo.Id,
                ordemServicoId: os.Id);

            await database.CleanupAsync(
                clienteId: outroCliente.Id);
        }
    }
}
