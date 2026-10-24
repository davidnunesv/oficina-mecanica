
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oficina.Infrastructure.Persistence;
using Oficina.IntegrationTests.Persistence;

namespace Oficina.IntegrationTests.Http;

public sealed class OficinaApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "Oficina.IntegrationTests";
    private const string Audience = "Oficina.IntegrationTests.Api";

    // Chave exclusivamente de teste; nunca usar na aplicação real.
    private const string TestSigningSecret =
        "Oficina.Testes.JWT.Segredo.Exclusivo.Para.Integracao.2026!";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes(TestSigningSecret));

    private readonly string _connectionString;

    public OficinaApiFactory(SqlServerTestDatabase database)
    {
        // Valida servidor e banco usando nosso helper existente.
        using var validationContext = database.CreateContext();

        _connectionString =
            Environment.GetEnvironmentVariable("OFICINA_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Configure OFICINA_TEST_CONNECTION_STRING.");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = Issuer,

                            ValidateAudience = true,
                            ValidAudience = Audience,

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = SigningKey,

                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero,

                            RoleClaimType = "role",
                            NameClaimType = "sub"
                        };
                });
        });

        base.ConfigureWebHost(builder);
    }

    public HttpClient CriarClienteSemToken()
    {
        ValidarBanco();

        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public HttpClient CriarClienteAutenticado(
        string role,
        Guid? clienteId = null)
    {
        var client = CriarClienteSemToken();

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString("N")),
            new("role", role)
        };

        if (clienteId.HasValue)
        {
            claims.Add(
                new Claim("cliente_id", clienteId.Value.ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                SigningKey,
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private void ValidarBanco()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<OficinaDbContext>();

        var banco = context.Database.GetDbConnection().Database;

        if (!string.Equals(
            banco,
            "OficinaMecanica_IntegrationTests",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Os testes HTTP devem utilizar exclusivamente o banco de integração.");
        }
    }
}
