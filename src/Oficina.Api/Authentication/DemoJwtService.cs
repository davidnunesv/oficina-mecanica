
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Oficina.Api.Authentication;

public sealed class DemoJwtService
{
    private const string Issuer = "Oficina.Demo";
    private const string Audience = "Oficina.Api";

    // Chave aleatória criada a cada inicialização da API.
    // Não é salva no Git, no Compose ou na imagem Docker.
    private readonly SymmetricSecurityKey _key =
        new(RandomNumberGenerator.GetBytes(32));

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,

        ValidateAudience = true,
        ValidAudience = Audience,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _key,

        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),

        RoleClaimType = "role",
        NameClaimType = "sub"
    };

    public string GerarToken(string role, Guid? clienteId = null)
    {
        var claims = new List<Claim>
        {
            new("sub", "avaliador-local"),
            new("role", role)
        };

        if (clienteId.HasValue)
        {
            claims.Add(new Claim(
                "cliente_id",
                clienteId.Value.ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddSeconds(-10),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                _key,
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
