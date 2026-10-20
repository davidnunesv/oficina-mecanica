
using Oficina.Application.Interfaces;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.UnitTests.UseCases.OrdensServico;

public class FinalizarOrdemServicoUseCaseTests
{
    [Fact]
    public async Task DeveFinalizarOsSemPecas()
    {
        var os = CriarOsEmExecucao();
        var (useCase, repositorioOs, repositorioPecas, transacao) = CriarContexto(os);

        var finalizado = await useCase.ExecuteAsync(os.Id);

        Assert.True(finalizado);
        Assert.Equal(StatusOS.Finalizada, os.Status);
        Assert.NotNull(os.DataFinalizacao);
        Assert.True(os.DataFinalizacao >= os.DataInicioExecucao);
        Assert.Equal(1, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
        Assert.Equal(1, transacao.ExecutionCount);
    }

    [Fact]
    public async Task DeveFinalizarOsEConsumirTodasAsPecas()
    {
        var peca1 = new PecaInsumo("Filtro de óleo", 35m);
        var peca2 = new PecaInsumo("Filtro de ar", 50m);
        var os = CriarOsEmExecucao(peca1, peca2);

        peca1.Reservar(os.Id);
        peca2.Reservar(os.Id);

        var (useCase, repositorioOs, repositorioPecas, transacao) = CriarContexto(os, peca1, peca2);

        var finalizado = await useCase.ExecuteAsync(os.Id);

        Assert.True(finalizado);
        Assert.Equal(StatusOS.Finalizada, os.Status);
        Assert.NotNull(os.DataFinalizacao);
        Assert.Equal(StatusPecaInsumo.Consumida, peca1.Status);
        Assert.Equal(StatusPecaInsumo.Consumida, peca2.Status);
        Assert.Equal(os.Id, peca1.OrdemServicoId);
        Assert.Equal(os.Id, peca2.OrdemServicoId);
        Assert.Equal(1, repositorioOs.UpdateCount);
        Assert.Equal(2, repositorioPecas.UpdateCount);
        Assert.Equal(1, transacao.ExecutionCount);
    }

    [Fact]
    public async Task NaoDeveFinalizarOsAntesDaExecucao()
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        var (useCase, repositorioOs, _, _) = CriarContexto(os);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(StatusOS.Recebida, os.Status);
        Assert.Null(os.DataFinalizacao);
        Assert.Equal(0, repositorioOs.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveFinalizarQuandoPecaNaoEstaReservada()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = CriarOsEmExecucao(peca);
        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os, peca);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveConsumirPecaReservadaParaOutraOs()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = CriarOsEmExecucao(peca);

        var outraOsId = Guid.NewGuid();
        peca.Reservar(outraOsId);

        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os, peca);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(StatusPecaInsumo.Reservada, peca.Status);
        Assert.Equal(outraOsId, peca.OrdemServicoId);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveFinalizarQuandoPecaNaoExiste()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = CriarOsEmExecucao(peca);

        // A peça consta na OS, mas não existe no repositório.
        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Null(os.DataFinalizacao);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveConsumirParcialmenteQuandoUmaPecaEstaIndisponivel()
    {
        var peca1 = new PecaInsumo("Filtro de óleo", 35m);
        var peca2 = new PecaInsumo("Filtro de ar", 50m);
        var os = CriarOsEmExecucao(peca1, peca2);

        peca1.Reservar(os.Id);
        // A segunda peça permanece disponível.

        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os, peca1, peca2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(StatusPecaInsumo.Reservada, peca1.Status);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca2.Status);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveFinalizarOsDuasVezes()
    {
        var os = CriarOsEmExecucao();
        var (useCase, repositorioOs, _, _) = CriarContexto(os);

        await useCase.ExecuteAsync(os.Id);

        var dataFinalizacao = os.DataFinalizacao;

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id));

        Assert.Equal(dataFinalizacao, os.DataFinalizacao);
        Assert.Equal(1, repositorioOs.UpdateCount);
    }

    [Fact]
    public async Task DeveRetornarFalsoQuandoOsNaoExiste()
    {
        var (useCase, _, _, _) = CriarContexto(null);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.False(resultado);
    }

    [Fact]
    public async Task NaoDeveAceitarIdVazio()
    {
        var (useCase, _, _, _) = CriarContexto(null);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Guid.Empty));
    }


    private static OrdemServico CriarOsEmExecucao(params PecaInsumo[] pecas)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);

        foreach (var peca in pecas)
            os.AdicionarPeca(peca.Id, peca.Preco);

        os.GerarOrcamento();
        os.AprovarOrcamento();
        os.IniciarExecucao();

        return os;
    }

    private static (FinalizarOrdemServicoUseCase UseCase, FakeOrdemServicoRepository Ordens, FakePecaInsumoRepository Pecas, FakeTransactionManager Transacao)
        CriarContexto(OrdemServico? os, params PecaInsumo[] pecas)
    {
        var repositorioOs = new FakeOrdemServicoRepository(os);
        var repositorioPecas = new FakePecaInsumoRepository(pecas);
        var transacao = new FakeTransactionManager();

        var useCase = new FinalizarOrdemServicoUseCase(repositorioOs, repositorioPecas, transacao);

        return (useCase, repositorioOs, repositorioPecas, transacao);
    }


    private sealed class FakeOrdemServicoRepository(OrdemServico? os) : IOrdemServicoRepository
    {
        public int UpdateCount { get; private set; }

        public Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(os?.Id == id ? os : null);
        }

        public Task UpdateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task CreateAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<OrdemServico>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakePecaInsumoRepository(params PecaInsumo[] pecas) : IPecaInsumoRepository
    {
        private readonly List<PecaInsumo> _pecas = pecas.ToList();

        public int UpdateCount { get; private set; }

        public Task<PecaInsumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pecas.FirstOrDefault(peca => peca.Id == id));
        }

        public Task UpdateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task CreateAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<PecaInsumo>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(PecaInsumo peca, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid pecaId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public int ExecutionCount { get; private set; }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return await operation(cancellationToken);
        }
    }


}
