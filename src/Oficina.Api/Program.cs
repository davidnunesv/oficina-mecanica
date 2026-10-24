
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Oficina.Api.Exceptions;
using Oficina.Application.Services;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Infrastructure;
using System.Security.Claims;
using Oficina.Api.Authentication;

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

var demoAuthEnabled =
    builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("DemoAuth:Enabled");

if (demoAuthEnabled)
{
    var demoJwt = new DemoJwtService();

    builder.Services.AddSingleton(demoJwt);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters =
                demoJwt.ValidationParameters;
        });
}
else
{
    // Mantém o comportamento JWT original.
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
}


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


// Endpoints para testar JWT localmente.
if (demoAuthEnabled)
{
    app.MapPost(
        "/dev/demo-token/admin",
        (DemoJwtService jwt) =>
        {
            return Results.Ok(new
            {
                accessToken = jwt.GerarToken("Admin"),
                tokenType = "Bearer",
                expiresIn = 3600
            });
        })
        .AllowAnonymous()
        .WithTags("Demonstração")
        .WithSummary("Gerar JWT Admin para testes locais");

    app.MapPost(
        "/dev/demo-token/cliente/{clienteId:guid}",
        (Guid clienteId, DemoJwtService jwt) =>
        {
            if (clienteId == Guid.Empty)
                return Results.BadRequest();

            return Results.Ok(new
            {
                accessToken = jwt.GerarToken(
                    "Cliente",
                    clienteId),
                tokenType = "Bearer",
                expiresIn = 3600
            });
        })
        .AllowAnonymous()
        .WithTags("Demonstração")
        .WithSummary("Gerar JWT Cliente para testes locais");
}


app.Run();

public partial class Program { }
