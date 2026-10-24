
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Interfaces;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Repositories;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<OficinaDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPecaInsumoRepository, PecaInsumoRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();

        services.AddScoped<ITransactionManager, EfTransactionManager>();


        return services;
    }
}
