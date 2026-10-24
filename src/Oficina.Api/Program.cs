
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Oficina.Api.Exceptions;
using Oficina.Application.Services;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Infrastructure;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DefaultConnection' não foi configurada.");

// Infrastructure
builder.Services.AddInfrastructure(connectionString);

// Application Services
builder.Services.AddScoped<ClienteApplicationService>();
builder.Services.AddScoped<VeiculoApplicationService>();
builder.Services.AddScoped<ServicoApplicationService>();
builder.Services.AddScoped<PecaInsumoApplicationService>();
builder.Services.AddScoped<OrdemServicoApplicationService>();

// Use Cases
builder.Services.AddScoped<AprovarOrcamentoUseCase>();
builder.Services.AddScoped<FinalizarOrdemServicoUseCase>();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Autenticação JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Autorização

builder.Services.AddAuthorization(options =>
{
    // Proteção por padrão para novos endpoints.
    options.FallbackPolicy = new AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));

    options.AddPolicy("ClienteOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Cliente")
              .RequireClaim("cliente_id"));
});



// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina Mecânica API",
        Version = "v1",
        Description = "API REST para gestão de uma oficina mecânica."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe somente o token JWT."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// Tratamento de exceções
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Oficina Mecânica API v1");

        options.DocumentTitle = "Oficina Mecânica - Swagger";
    });
}

app.UseHttpsRedirection();

// A ordem desses middlewares é importante.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoints temporários para testar JWT localmente.
// Eles não existirão nos ambientes de produção.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/auth/me", (ClaimsPrincipal user) =>
        Results.Ok(new
        {
            nome = user.Identity?.Name,
            autenticado = user.Identity?.IsAuthenticated,
            administrador = user.IsInRole("Admin")
        }))
        .RequireAuthorization();

    app.MapGet("/dev/auth/admin", () =>
        Results.Ok(new
        {
            mensagem = "Acesso administrativo autorizado."
        }))
        .RequireAuthorization("AdminOnly");
}

app.Run();

public partial class Program { }
