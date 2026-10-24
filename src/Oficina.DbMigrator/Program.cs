
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oficina.Infrastructure.Persistence;

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string da migração não foi configurada.");

var options = new DbContextOptionsBuilder<OficinaDbContext>()
    .UseSqlServer(connectionString)
    .Options;

for (var tentativa = 1; tentativa <= 30; tentativa++)
{
    try
    {
        await using var context = new OficinaDbContext(options);

        Console.WriteLine("Aplicando migrations do Entity Framework...");

        await context.Database.MigrateAsync();

        Console.WriteLine("Migrations aplicadas com sucesso.");

        return;
    }
    catch (SqlException ex) when (tentativa < 30)
    {
        Console.Error.WriteLine(
            $"SQL Server indisponível (tentativa {tentativa}/30, código {ex.Number}).");

        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
