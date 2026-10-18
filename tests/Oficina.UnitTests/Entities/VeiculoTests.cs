
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.Entities;

public class VeiculoTests
{
    [Fact]
    public void DeveCriarVeiculoValido()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var placa = new Placa("ABC-1234");

        // Act
        var veiculo = new Veiculo(
            clienteId,
            placa,
            "Ford",
            "Ka",
            2019);

        // Assert
        Assert.NotEqual(Guid.Empty, veiculo.Id);
        Assert.Equal(clienteId, veiculo.ClienteId);
        Assert.Equal(placa, veiculo.Placa);
        Assert.Equal("Ford", veiculo.Marca);
        Assert.Equal("Ka", veiculo.Modelo);
        Assert.Equal(2019, veiculo.Ano);
    }

    [Fact]
    public void NaoDevePermitirClienteIdVazio()
    {
        var placa = new Placa("ABC1234");

        Assert.Throws<ArgumentException>(() =>
            new Veiculo(
                Guid.Empty, placa, "Ford", "Ka", 2019));
    }

    [Fact]
    public void NaoDevePermitirPlacaNula()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Veiculo(
                Guid.NewGuid(), null!, "Ford", "Ka", 2019));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirMarcaVazia(string marca)
    {
        Assert.Throws<ArgumentException>(() =>
            new Veiculo(
                Guid.NewGuid(),
                new Placa("ABC1234"),
                marca,
                "Ka",
                2019));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirModeloVazio(string modelo)
    {
        Assert.Throws<ArgumentException>(() =>
            new Veiculo(
                Guid.NewGuid(),
                new Placa("ABC1234"),
                "Ford",
                modelo,
                2019));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NaoDevePermitirAnoInvalido(int ano)
    {
        Assert.Throws<ArgumentException>(() =>
            new Veiculo(
                Guid.NewGuid(),
                new Placa("ABC1234"),
                "Ford",
                "Ka",
                ano));
    }

    [Fact]
    public void DeveRemoverEspacosDeMarcaEModelo()
    {
        var veiculo = new Veiculo(
            Guid.NewGuid(),
            new Placa("BRA2E19"),
            "  Ford  ",
            "  Ka  ",
            2019);

        Assert.Equal("Ford", veiculo.Marca);
        Assert.Equal("Ka", veiculo.Modelo);
    }


    [Fact]
    public void DeveAtualizarDadosDoVeiculo()
    {
        var veiculo = new Veiculo(Guid.NewGuid(), new Placa("ABC1234"), "Ford", "Ka", 2019);

        veiculo.Update(new Placa("BRA2E19"), "Fiat", "Uno", 2020);

        Assert.Equal("BRA2E19", veiculo.Placa.Valor);
        Assert.Equal("Fiat", veiculo.Marca);
        Assert.Equal("Uno", veiculo.Modelo);
        Assert.Equal(2020, veiculo.Ano);
    }

    [Fact]
    public void NaoDeveAlterarDadosQuandoMarcaForInvalida()
    {
        var veiculo = new Veiculo(Guid.NewGuid(), new Placa("ABC1234"), "Ford", "Ka", 2019);

        Assert.Throws<ArgumentException>(() => veiculo.Update(new Placa("BRA2E19"), "", "Uno", 2020));

        Assert.Equal("ABC1234", veiculo.Placa.Valor);
        Assert.Equal("Ford", veiculo.Marca);
        Assert.Equal("Ka", veiculo.Modelo);
        Assert.Equal(2019, veiculo.Ano);
    }

}
