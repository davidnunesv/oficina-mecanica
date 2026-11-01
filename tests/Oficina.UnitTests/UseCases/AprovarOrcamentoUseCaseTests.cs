
using Oficina.Application.Interfaces;
using Oficina.Application.UseCases.OrdensServico;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.UnitTests.UseCases.OrdensServico;

public class AprovarOrcamentoUseCaseTests
{
    [Fact]
    public async Task DeveAprovarOrcamentoSemPecas()
    {
        var os = CriarOsComOrcamento();
        var (useCase, repositorioOs, repositorioPecas, transacao) = CriarContexto(os);

        var aprovado = await useCase.ExecuteAsync(os.Id, os.ClienteId);

        Assert.True(aprovado);
        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.NotNull(os.DataInicioExecucao);
        Assert.Equal(1, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
        Assert.Equal(1, transacao.ExecutionCount);
    }

    [Fact]
    public async Task DeveAprovarOrcamentoEReservarTodasAsPecas()
    {
        var peca1 = new PecaInsumo("Filtro de óleo", 35m);
        var peca2 = new PecaInsumo("Filtro de ar", 50m);
        var os = CriarOsComOrcamento(peca1, peca2);
        var (useCase, repositorioOs, repositorioPecas, transacao) = CriarContexto(os, peca1, peca2);

        var aprovado = await useCase.ExecuteAsync(os.Id, os.ClienteId);

        Assert.True(aprovado);
        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(StatusPecaInsumo.Reservada, peca1.Status);
        Assert.Equal(StatusPecaInsumo.Reservada, peca2.Status);
        Assert.Equal(os.Id, peca1.OrdemServicoId);
        Assert.Equal(os.Id, peca2.OrdemServicoId);
        Assert.Equal(1, repositorioOs.UpdateCount);
        Assert.Equal(2, repositorioPecas.UpdateCount);
        Assert.Equal(1, transacao.ExecutionCount);
    }

    [Fact]
    public async Task NaoDeveAprovarOrcamentoRejeitado()
    {
        var os = CriarOsComOrcamento();
        os.RejeitarOrcamento();
        var (useCase, repositorioOs, _, _) = CriarContexto(os);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id, os.ClienteId));

        Assert.Equal(StatusOrcamento.Rejeitado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveReservarParcialmenteQuandoUmaPecaEstiverIndisponivel()
    {
        var peca1 = new PecaInsumo("Filtro de óleo", 35m);
        var peca2 = new PecaInsumo("Filtro de ar", 50m);
        var os = CriarOsComOrcamento(peca1, peca2);

        peca2.Reservar(Guid.NewGuid());

        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os, peca1, peca2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id, os.ClienteId));

        Assert.Equal(StatusPecaInsumo.Disponivel, peca1.Status);
        Assert.Null(peca1.OrdemServicoId);
        Assert.Equal(StatusPecaInsumo.Reservada, peca2.Status);
        Assert.Equal(StatusOrcamento.Pendente, os.Orcamento!.Status);
        Assert.Equal(StatusOS.AguardandoAprovacao, os.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAprovarQuandoPecaNaoExiste()
    {
        var peca = new PecaInsumo("Filtro de óleo", 35m);
        var os = CriarOsComOrcamento(peca);

        // A peça está na OS, mas não existe no repositório.
        var (useCase, repositorioOs, repositorioPecas, _) = CriarContexto(os);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(os.Id, os.ClienteId));

        Assert.Equal(StatusOrcamento.Pendente, os.Orcamento!.Status);
        Assert.Equal(StatusPecaInsumo.Disponivel, peca.Status);
        Assert.Equal(0, repositorioOs.UpdateCount);
        Assert.Equal(0, repositorioPecas.UpdateCount);
    }

    [Fact]
    public async Task NaoDeveAprovarOrcamentoDuasVezes()
    {
        var os = CriarOsComOrcamento();
        var (useCase, repositorioOs, _, _) = CriarContexto(os);

        await useCase.ExecuteAsync(os.Id, os.ClienteId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(os.Id, os.ClienteId));

        Assert.Equal(StatusOrcamento.Aprovado, os.Orcamento!.Status);
        Assert.Equal(StatusOS.EmExecucao, os.Status);
        Assert.Equal(1, repositorioOs.UpdateCount);
    }

    [Fact]
    public async Task DeveRetornarFalsoQuandoOsNaoExiste()
    {
        var (useCase, _, _, _) = CriarContexto(null);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(resultado);
    }

    [Fact]
    public async Task NaoDeveAceitarIdVazio()
    {
        var (useCase, _, _, _) = CriarContexto(null);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Guid.Empty, Guid.Empty));
    }


    private static OrdemServico CriarOsComOrcamento(params PecaInsumo[] pecas)
    {
        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());

        os.CreateDiagnostic();
        os.AdicionarServico(Guid.NewGuid(), 1, 100m);

        foreach (var peca in pecas)
            os.AdicionarPeca(peca.Id, peca.Preco);

        os.GerarOrcamento();

        return os;
    }

    private static (AprovarOrcamentoUseCase UseCase, FakeOrdemServicoRepository Ordens, FakePecaInsumoRepository Pecas, FakeTransactionManager Transacao)
        CriarContexto(OrdemServico? os, params PecaInsumo[] pecas)
    {
        var repositorioOs = new FakeOrdemServicoRepository(os);
        var repositorioPecas = new FakePecaInsumoRepository(pecas);
        var transacao = new FakeTransactionManager();

        var useCase = new AprovarOrcamentoUseCase(repositorioOs, repositorioPecas, transacao);

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
