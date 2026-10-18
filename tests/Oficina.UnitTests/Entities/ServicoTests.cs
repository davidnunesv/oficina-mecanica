using Oficina.Domain.Entities;

namespace Oficina.UnitTests.Entities;

public class ServicoTests
{
    [Fact]
    public void DeveCriarServicoValido()
    {
        // Act
        var servico = new Servico("Troca de óleo", "Substituição do óleo do motor",  120.50m);

        // Assert
        Assert.NotEqual(Guid.Empty, servico.Id);
        Assert.Equal("Troca de óleo", servico.Nome);
        Assert.Equal("Substituição do óleo do motor", servico.Descricao);
        Assert.Equal(120.50m, servico.Preco);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirNomeVazio(string nome)
    {
        Assert.Throws<ArgumentException>(() =>
            new Servico(nome, "Descrição", 100m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirDescricaoVazia(
        string descricao)
    {
        Assert.Throws<ArgumentException>(() =>
            new Servico("Troca de óleo", descricao, 100m));
    }

    [Fact]
    public void NaoDevePermitirPrecoZero()
    {
        Assert.Throws<ArgumentException>(() =>
            new Servico("Alinhamento", "Descrição", 0m));
    }

    [Fact]
    public void NaoDevePermitirPrecoNegativo()
    {
        Assert.Throws<ArgumentException>(() =>
            new Servico("Alinhamento", "Descrição", -50m));
    }

    [Fact]
    public void DeveRemoverEspacosDoNomeEDescricao()
    {
        var servico = new Servico("  Alinhamento  ", "  Alinhamento das rodas  ", 100m);

        Assert.Equal("Alinhamento", servico.Nome);
        Assert.Equal("Alinhamento das rodas", servico.Descricao);
    }
}
