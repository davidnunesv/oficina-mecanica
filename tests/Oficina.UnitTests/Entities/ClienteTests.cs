using Oficina.Domain.Entities;
using Oficina.Domain.Enums;
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


    [Fact]
    public void DeveAtualizarDadosDoCliente()
    {
        var cliente = new Cliente("David Nunes", new Documento("52998224725"));
        var novoDocumento = new Documento("12ABC34501DE35");

        cliente.Update("David Vieira", novoDocumento);

        Assert.Equal("David Vieira", cliente.Nome);
        Assert.Equal(novoDocumento, cliente.Documento);
        Assert.Equal("12ABC34501DE35", cliente.Documento.Valor);
    }

    [Fact]
    public void DeveAtualizarNomeMantendoDocumento()
    {
        var documento = new Documento("52998224725");
        var cliente = new Cliente("David Nunes", documento);

        cliente.Update("David Vieira", documento);

        Assert.Equal("David Vieira", cliente.Nome);
        Assert.Equal(documento, cliente.Documento);
    }

    [Fact]
    public void DeveRemoverEspacosAoAtualizarNome()
    {
        var cliente = new Cliente("David Nunes", new Documento("52998224725"));

        cliente.Update("  David Vieira  ", cliente.Documento);

        Assert.Equal("David Vieira", cliente.Nome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NaoDeveAtualizarClienteComNomeInvalido(string? nome)
    {
        var documentoOriginal = new Documento("52998224725");
        var cliente = new Cliente("David Nunes", documentoOriginal);
        var novoDocumento = new Documento("12ABC34501DE35");

        Assert.Throws<ArgumentException>(() => cliente.Update(nome!, novoDocumento));

        Assert.Equal("David Nunes", cliente.Nome);
        Assert.Equal(documentoOriginal, cliente.Documento);
    }

    [Fact]
    public void NaoDeveAtualizarClienteComDocumentoNulo()
    {
        var documentoOriginal = new Documento("52998224725");
        var cliente = new Cliente("David Nunes", documentoOriginal);

        Assert.Throws<ArgumentNullException>(() => cliente.Update("David Vieira", null!));

        Assert.Equal("David Nunes", cliente.Nome);
        Assert.Equal(documentoOriginal, cliente.Documento);
    }

}
