
using Oficina.Application.Interfaces;
using Oficina.Application.Services;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.UnitTests.Services;

public class PecaInsumoApplicationServiceTests
{
    [Fact]
    public async Task DeveCadastrarPecaDisponivel()
    {
        var (service, repositorio) = CriarContexto();

        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        var peca = Assert.Single(repositorio.Pecas);

        Assert.Equal(pecaId, peca.Id);
        Assert.Equal("Filtro de óleo", peca.Nome);
        Assert.Equal(35m, peca.Preco);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Null(peca.OrdemServicoId);
    }

    [Fact]
    public async Task DevePermitirMultiplasUnidadesDoMesmoProduto()
    {
        var (service, repositorio) = CriarContexto();

        var primeiroId = await service.CreateAsync("Filtro de óleo", 35m);
        var segundoId = await service.CreateAsync("Filtro de óleo", 35m);

        Assert.NotEqual(primeiroId, segundoId);
        Assert.Equal(2, repositorio.Pecas.Count);
    }

    [Fact]
    public async Task NaoDeveCadastrarPecaComDadosInvalidos()
    {
        var (service, repositorio) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("", 35m));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("Filtro de óleo", 0m));

        Assert.Empty(repositorio.Pecas);
    }

    [Fact]
    public async Task DeveConsultarPecaPorId()
    {
        var (service, _) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        var resultado = await service.GetByIdAsync(pecaId);

        Assert.NotNull(resultado);
        Assert.Equal(pecaId, resultado.Id);
        Assert.Equal("Filtro de óleo", resultado.Nome);
        Assert.Equal(35m, resultado.Preco);
        Assert.Equal("Disponivel", resultado.Status);
        Assert.Null(resultado.OrdemServicoId);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoPecaNaoExiste()
    {
        var (service, _) = CriarContexto();

        var resultado = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DeveListarPecas()
    {
        var (service, _) = CriarContexto();

        await service.CreateAsync("Filtro de óleo", 35m);
        await service.CreateAsync("Filtro de ar", 50m);

        var resultado = await service.ListAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, peca => peca.Nome == "Filtro de óleo");
        Assert.Contains(resultado, peca => peca.Nome == "Filtro de ar");
    }

    [Fact]
    public async Task DeveRetornarListaVazia()
    {
        var (service, _) = CriarContexto();

        var resultado = await service.ListAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task DeveConsultarStatusDePecaReservada()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        Assert.Single(repositorio.Pecas).Reservar(ordemServicoId);

        var resultado = await service.GetByIdAsync(pecaId);

        Assert.NotNull(resultado);
        Assert.Equal("Reservada", resultado.Status);
        Assert.Equal(ordemServicoId, resultado.OrdemServicoId);
    }

    [Fact]
    public async Task DeveAtualizarPecaDisponivel()
    {
        var (service, _) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        var atualizado = await service.UpdateAsync(pecaId, "Filtro de ar", 50m);
        var peca = await service.GetByIdAsync(pecaId);

        Assert.True(atualizado);
        Assert.NotNull(peca);
        Assert.Equal(pecaId, peca.Id);
        Assert.Equal("Filtro de ar", peca.Nome);
        Assert.Equal(50m, peca.Preco);
        Assert.Equal("Disponivel", peca.Status);
    }

    [Fact]
    public async Task NaoDeveAtualizarPecaInexistente()
    {
        var (service, _) = CriarContexto();

        var atualizado = await service.UpdateAsync(Guid.NewGuid(), "Filtro de ar", 50m);

        Assert.False(atualizado);
    }

    [Fact]
    public async Task NaoDeveAtualizarPecaComPrecoInvalido()
    {
        var (service, _) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(pecaId, "Filtro de ar", 0m));

        var peca = await service.GetByIdAsync(pecaId);

        Assert.Equal("Filtro de óleo", peca!.Nome);
        Assert.Equal(35m, peca.Preco);
    }

    [Fact]
    public async Task NaoDeveAtualizarPecaReservada()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        Assert.Single(repositorio.Pecas).Reservar(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(pecaId, "Filtro de ar", 50m));

        Assert.Equal(35m, (await service.GetByIdAsync(pecaId))!.Preco);
    }

    [Fact]
    public async Task NaoDeveAtualizarPecaConsumida()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        var peca = Assert.Single(repositorio.Pecas);
        peca.Reservar(ordemServicoId);
        peca.Consumir(ordemServicoId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(pecaId, "Filtro de ar", 50m));

        Assert.Equal(35m, (await service.GetByIdAsync(pecaId))!.Preco);
    }

    [Fact]
    public async Task DeveExcluirPecaDisponivelSemVinculos()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        var excluido = await service.DeleteAsync(pecaId);

        Assert.True(excluido);
        Assert.Null(await service.GetByIdAsync(pecaId));
        Assert.Empty(repositorio.Pecas);
    }

    [Fact]
    public async Task NaoDeveExcluirPecaInexistente()
    {
        var (service, _) = CriarContexto();

        var excluido = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(excluido);
    }

    [Fact]
    public async Task NaoDeveExcluirPecaVinculadaAOs()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        repositorio.PecasComDependencias.Add(pecaId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(pecaId));

        Assert.Single(repositorio.Pecas);
        Assert.Equal("Disponivel", (await service.GetByIdAsync(pecaId))!.Status);
    }

    [Fact]
    public async Task NaoDeveExcluirPecaReservada()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);

        Assert.Single(repositorio.Pecas).Reservar(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(pecaId));

        Assert.Single(repositorio.Pecas);
    }

    [Fact]
    public async Task NaoDeveExcluirPecaConsumida()
    {
        var (service, repositorio) = CriarContexto();
        var pecaId = await service.CreateAsync("Filtro de óleo", 35m);
        var ordemServicoId = Guid.NewGuid();

        var peca = Assert.Single(repositorio.Pecas);
        peca.Reservar(ordemServicoId);
        peca.Consumir(ordemServicoId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(pecaId));

        Assert.Single(repositorio.Pecas);
    }

    [Fact]
    public async Task NaoDeveAceitarIdVazio()
    {
        var (service, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.Empty, "Filtro de óleo", 35m));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteAsync(Guid.Empty));
    }


    private static (PecaInsumoApplicationService Service, FakePecaInsumoRepository Repositorio) CriarContexto()
    {
        var repositorio = new FakePecaInsumoRepository();
        var service = new PecaInsumoApplicationService(repositorio);

        return (service, repositorio);
    }

    private sealed class FakePecaInsumoRepository : IPecaInsumoRepository
    {
        private readonly List<PecaInsumo> _pecas = new();

        public IReadOnlyList<PecaInsumo> Pecas => _pecas.AsReadOnly();
        public HashSet<Guid> PecasComDependencias { get; } = new();

        public Task CreateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
        {
            _pecas.Add(peca);
            return Task.CompletedTask;
        }

        public Task<PecaInsumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pecas.FirstOrDefault(peca => peca.Id == id));
        }

        public Task<IReadOnlyList<PecaInsumo>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PecaInsumo>>(_pecas.ToList());
        }

        public Task UpdateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
        {
            _pecas.Remove(peca);
            return Task.CompletedTask;
        }

        public Task<bool> HasDependenciesAsync(Guid pecaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PecasComDependencias.Contains(pecaId));
        }
    }

}
