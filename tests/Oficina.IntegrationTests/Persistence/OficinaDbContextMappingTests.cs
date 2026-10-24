
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Entities;
using Oficina.Infrastructure.Persistence;
using Xunit;

namespace Oficina.IntegrationTests.Persistence;

public class OficinaDbContextMappingTests
{
    [Fact]
    public void DeveConstruirModeloComTodasAsEntidades()
    {
        using var context = CriarContexto();

        var model = context.Model;

        Assert.NotNull(model.FindEntityType(typeof(Cliente)));
        Assert.NotNull(model.FindEntityType(typeof(Veiculo)));
        Assert.NotNull(model.FindEntityType(typeof(Servico)));
        Assert.NotNull(model.FindEntityType(typeof(PecaInsumo)));
        Assert.NotNull(model.FindEntityType(typeof(OrdemServico)));
        Assert.NotNull(model.FindEntityType(typeof(Diagnostico)));
        Assert.NotNull(model.FindEntityType(typeof(Orcamento)));
        Assert.NotNull(model.FindEntityType(typeof(ItemServico)));
        Assert.NotNull(model.FindEntityType(typeof(ItemPecaOS)));
    }

    [Fact]
    public void DeveMapearColecoesPrivadasDaOrdemServico()
    {
        using var context = CriarContexto();

        var entidade = context.Model.FindEntityType(typeof(OrdemServico))!;

        var servicos = entidade.FindNavigation(nameof(OrdemServico.ItensServico));
        var pecas = entidade.FindNavigation(nameof(OrdemServico.ItensPeca));

        Assert.Equal("_itensServico", servicos!.FieldInfo?.Name);
        Assert.Equal("_itensPeca", pecas!.FieldInfo?.Name);
    }

    [Fact]
    public void DeveConfigurarConcorrenciaNasPecas()
    {
        using var context = CriarContexto();

        var entidade = context.Model.FindEntityType(typeof(PecaInsumo))!;
        var rowVersion = entidade.FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
    }

    private static OficinaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True;")
            .Options;

        return new OficinaDbContext(options);
    }
}
