
using Microsoft.OpenApi;
using Oficina.Api.Exceptions;
using Oficina.Application.Services;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi configurada.");

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

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina Mecânica API",
        Version = "v1",
        Description = "API REST para gerenciamento de clientes, veículos, serviços, peças e ordens de serviço de uma oficina mecânica."
    });
});

builder.Services.AddAuthorization();

// Tratamento global de exceções
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina Mecânica API v1");
        options.DocumentTitle = "Oficina Mecânica - Swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
