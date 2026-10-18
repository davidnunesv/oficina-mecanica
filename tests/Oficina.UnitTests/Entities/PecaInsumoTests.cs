using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.UnitTests.Entities;

public class PecaInsumoTests
{
    [Fact]
    public void DeveCriarPecaDisponivel()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);

        Assert.NotEqual(Guid.Empty, peca.Id);
        Assert.Equal("Filtro de óleo", peca.Nome);
        Assert.Equal(35m, peca.Preco);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Null(peca.OrdemServicoId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoDevePermitirNomeVazio(string nome)
    {
        Assert.Throws<ArgumentException>(() => new PecaInsumo(nome, 35m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NaoDevePermitirPrecoInvalido(decimal preco)
    {
        Assert.Throws<ArgumentException>(() => new PecaInsumo("Filtro de óleo", preco));
    }

    [Fact]
    public void DeveReservarPecaDisponivel()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        peca.Reservar(ordemServicoId);

        Assert.Equal(StatusPecaInsumo.Reservada, peca.Status);
        Assert.Equal(ordemServicoId, peca.OrdemServicoId);
    }

    [Fact]
    public void NaoDevePermitirReservaDuplicada()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        peca.Reservar(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => peca.Reservar(Guid.NewGuid()));
    }

    [Fact]
    public void NaoDevePermitirReservaComOsVazia()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);

        Assert.Throws<ArgumentException>(() => peca.Reservar(Guid.Empty));
    }

    [Fact]
    public void NaoDevePermitirConsumirPecaDisponivel()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);

        Assert.Throws<InvalidOperationException>(() => peca.Consumir(Guid.NewGuid()));
    }

    [Fact]
    public void NaoDevePermitirConsumoPorOutraOs()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        peca.Reservar(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => peca.Consumir(Guid.NewGuid()));
    }

    [Fact]
    public void DeveConsumirPecaReservada()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        peca.Reservar(ordemServicoId);
        peca.Consumir(ordemServicoId);

        Assert.Equal(StatusPecaInsumo.Consumida, peca.Status);
        Assert.Equal(ordemServicoId, peca.OrdemServicoId);
    }

    [Fact]
    public void NaoDevePermitirConsumirDuasVezes()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        peca.Reservar(ordemServicoId);
        peca.Consumir(ordemServicoId);

        Assert.Throws<InvalidOperationException>(() => peca.Consumir(ordemServicoId));
    }
}
