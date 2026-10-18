using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.Entities;

public class ClienteTests
{
    [Fact]
    public void DeveCriarClienteComCpfValido()
    {
        // Arrange
        var documento = new Documento("529.982.247-25");

        // Act
        var cliente = new Cliente("David Nunes", documento);

        // Assert
        Assert.NotEqual(Guid.Empty, cliente.Id);
        Assert.Equal("David Nunes", cliente.Nome);
        Assert.Equal(documento, cliente.Documento);
        Assert.Equal(TipoDocumento.Cpf, cliente.Documento.Tipo);
    }

    [Fact]
    public void DeveCriarClienteComCnpjAlfanumerico()
    {
        // Arrange
        var documento = new Documento("12.ABC.345/01DE-35");

        // Act
        var cliente = new Cliente("Oficina X", documento);

        // Assert
        Assert.Equal(TipoDocumento.Cnpj, cliente.Documento.Tipo);
        Assert.Equal("12ABC34501DE35", cliente.Documento.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirNomeVazio(string nome)
    {
        // Arrange
        var documento = new Documento("52998224725");

        // Act e Assert
        Assert.Throws<ArgumentException>(
            () => new Cliente(nome, documento));
    }

    [Fact]
    public void NaoDevePermitirDocumentoNulo()
    {
        // Act e Assert
        Assert.Throws<ArgumentNullException>(
            () => new Cliente("David Nunes", null!));
    }

    [Fact]
    public void DeveRemoverEspacosDoNome()
    {
        // Arrange
        var documento = new Documento("52998224725");

        // Act
        var cliente = new Cliente("  David Nunes  ", documento);

        // Assert
        Assert.Equal("David Nunes", cliente.Nome);
    }
}
