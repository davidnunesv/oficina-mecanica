
using Oficina.Application.Interfaces;
using Oficina.Application.Services;
using Oficina.Domain.Entities;

namespace Oficina.UnitTests.Services;

public class ServicoApplicationServiceTests
{
    [Fact]
    public async Task DeveCadastrarServico()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);

        var servicoId = await service.CreateAsync("Troca de óleo", "Substituição do óleo do motor", 120m);

        var servico = Assert.Single(repositorio.Servicos);

        Assert.Equal(servicoId, servico.Id);
        Assert.Equal("Troca de óleo", servico.Nome);
        Assert.Equal("Substituição do óleo do motor", servico.Descricao);
        Assert.Equal(120m, servico.Preco);
    }

    [Fact]
    public async Task NaoDeveCadastrarServicoComNomeInvalido()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("", "Descrição", 100m));

        Assert.Empty(repositorio.Servicos);
    }

    [Fact]
    public async Task NaoDeveCadastrarServicoComPrecoInvalido()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("Alinhamento", "Descrição", -50m));

        Assert.Empty(repositorio.Servicos);
    }

    [Fact]
    public async Task DeveConsultarServicoPorId()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);
        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        var resultado = await service.GetByIdAsync(servicoId);

        Assert.NotNull(resultado);
        Assert.Equal(servicoId, resultado.Id);
        Assert.Equal("Alinhamento", resultado.Nome);
        Assert.Equal("Alinhamento das rodas", resultado.Descricao);
        Assert.Equal(100m, resultado.Preco);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoServicoNaoExiste()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        var resultado = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DeveListarServicos()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);
        await service.CreateAsync("Balanceamento", "Balanceamento das rodas", 80m);

        var resultado = await service.ListAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, servico => servico.Nome == "Alinhamento");
        Assert.Contains(resultado, servico => servico.Nome == "Balanceamento");
    }

    [Fact]
    public async Task DeveRetornarListaVazia()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        var resultado = await service.ListAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task DeveAtualizarServico()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());
        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        var atualizado = await service.UpdateAsync(servicoId, "Balanceamento", "Balanceamento das rodas", 120m);
        var servico = await service.GetByIdAsync(servicoId);

        Assert.True(atualizado);
        Assert.NotNull(servico);
        Assert.Equal(servicoId, servico.Id);
        Assert.Equal("Balanceamento", servico.Nome);
        Assert.Equal("Balanceamento das rodas", servico.Descricao);
        Assert.Equal(120m, servico.Preco);
    }

    [Fact]
    public async Task NaoDeveAtualizarServicoInexistente()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        var atualizado = await service.UpdateAsync(Guid.NewGuid(), "Alinhamento", "Descrição", 100m);

        Assert.False(atualizado);
    }

    [Fact]
    public async Task NaoDeveAtualizarServicoComDadosInvalidos()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());
        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(servicoId, "Balanceamento", "Nova descrição", 0m));

        var servico = await service.GetByIdAsync(servicoId);

        Assert.NotNull(servico);
        Assert.Equal("Alinhamento", servico.Nome);
        Assert.Equal("Alinhamento das rodas", servico.Descricao);
        Assert.Equal(100m, servico.Preco);
    }

    [Fact]
    public async Task DeveExcluirServicoSemDependencias()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);
        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        var excluido = await service.DeleteAsync(servicoId);

        Assert.True(excluido);
        Assert.Null(await service.GetByIdAsync(servicoId));
        Assert.Empty(repositorio.Servicos);
    }

    [Fact]
    public async Task NaoDeveExcluirServicoInexistente()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        var excluido = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(excluido);
    }

    [Fact]
    public async Task NaoDeveExcluirServicoComDependencias()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);
        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        repositorio.ServicosComDependencias.Add(servicoId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(servicoId));

        Assert.Single(repositorio.Servicos);
        Assert.NotNull(await service.GetByIdAsync(servicoId));
    }

    [Fact]
    public async Task NaoDeveAceitarIdVazio()
    {
        var service = new ServicoApplicationService(new FakeServicoRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.Empty, "Alinhamento", "Descrição", 100m));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteAsync(Guid.Empty));
    }


    [Fact]
    public async Task AtualizarPrecoDoCatalogoNaoDeveAlterarPrecoDeOsExistente()
    {
        var repositorio = new FakeServicoRepository();
        var service = new ServicoApplicationService(repositorio);

        var servicoId = await service.CreateAsync("Alinhamento", "Alinhamento das rodas", 100m);

        var os = new OrdemServico(Guid.NewGuid(), Guid.NewGuid());
        os.IniciarDiagnostico();
        os.AdicionarServico(servicoId, 1, 100m);

        await service.UpdateAsync(servicoId, "Alinhamento", "Alinhamento das rodas", 120m);

        var servicoAtualizado = await service.GetByIdAsync(servicoId);

        Assert.Equal(120m, servicoAtualizado!.Preco);
        Assert.Equal(100m, Assert.Single(os.ItensServico).PrecoAplicado);
    }



    private sealed class FakeServicoRepository : IServicoRepository
    {
        private readonly List<Servico> _servicos = new();

        public IReadOnlyList<Servico> Servicos => _servicos.AsReadOnly();
        public HashSet<Guid> ServicosComDependencias { get; } = new();

        public Task CreateAsync(Servico servico, CancellationToken cancellationToken = default)
        {
            _servicos.Add(servico);
            return Task.CompletedTask;
        }

        public Task<Servico?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_servicos.FirstOrDefault(servico => servico.Id == id));
        }

        public Task<IReadOnlyList<Servico>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Servico>>(_servicos.ToList());
        }

        public Task UpdateAsync(Servico servico, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Servico servico, CancellationToken cancellationToken = default)
        {
            _servicos.Remove(servico);
            return Task.CompletedTask;
        }

        public Task<bool> HasDependenciesAsync(Guid servicoId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ServicosComDependencias.Contains(servicoId));
        }
    }

}
