using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.ValueObjects;

public class PlacaTests
{
    [Theory]
    [InlineData("ABC-1234", "ABC1234")]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("BRA2E19", "BRA2E19")]
    [InlineData("bra2e19", "BRA2E19")]
    public void DeveAceitarPlacaValida(
        string valor,
        string valorEsperado)
    {
        // Act
        var placa = new Placa(valor);

        // Assert
        Assert.Equal(valorEsperado, placa.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC123")]
    [InlineData("AB1234")]
    [InlineData("ABC12D4")]
    [InlineData("ABC@1234")]
    [InlineData("1234567")]
    public void DeveRejeitarPlacaInvalida(string valor)
    {
        // Act e Assert
        Assert.Throws<ArgumentException>(
            () => new Placa(valor));
    }

    [Fact]
    public void PlacasComMesmoValorDevemSerIguais()
    {
        // Arrange
        var placa1 = new Placa("ABC-1234");
        var placa2 = new Placa("abc1234");

        // Assert
        Assert.Equal(placa1, placa2);
    }
}
