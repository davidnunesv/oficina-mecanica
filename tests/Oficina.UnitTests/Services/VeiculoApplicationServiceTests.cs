
using Oficina.Application.Interfaces;
using Oficina.Application.Services;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.Services;

public class VeiculoApplicationServiceTests
{
    [Fact]
    public async Task DeveCadastrarVeiculo()
    {
        var (service, repositorio, cliente) = CriarContexto();

        var veiculoId = await service.CreateAsync(cliente.Id, "ABC-1234", "  Ford  ", "  Ka  ", 2019);

        var veiculo = Assert.Single(repositorio.Veiculos);

        Assert.Equal(veiculoId, veiculo.Id);
        Assert.Equal(cliente.Id, veiculo.ClienteId);
        Assert.Equal("ABC1234", veiculo.Placa.Valor);
        Assert.Equal("Ford", veiculo.Marca);
        Assert.Equal("Ka", veiculo.Modelo);
        Assert.Equal(2019, veiculo.Ano);
    }

    [Fact]
    public async Task NaoDeveCadastrarVeiculoComClienteInexistente()
    {
        var (service, repositorio, _) = CriarContexto();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), "ABC1234", "Ford", "Ka", 2019));

        Assert.Empty(repositorio.Veiculos);
    }

    [Fact]
    public async Task NaoDeveCadastrarPlacaDuplicada()
    {
        var (service, repositorio, cliente) = CriarContexto();

        await service.CreateAsync(cliente.Id, "ABC-1234", "Ford", "Ka", 2019);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(cliente.Id, "abc1234", "Fiat", "Uno", 2020));

        Assert.Single(repositorio.Veiculos);
    }

    [Fact]
    public async Task NaoDeveCadastrarPlacaInvalida()
    {
        var (service, repositorio, cliente) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(cliente.Id, "ABC123", "Ford", "Ka", 2019));

        Assert.Empty(repositorio.Veiculos);
    }

    [Fact]
    public async Task DeveConsultarVeiculoPorId()
    {
        var (service, _, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);

        var resultado = await service.GetByIdAsync(veiculoId);

        Assert.NotNull(resultado);
        Assert.Equal(veiculoId, resultado.Id);
        Assert.Equal(cliente.Id, resultado.ClienteId);
        Assert.Equal("ABC1234", resultado.Placa);
        Assert.Equal("Ford", resultado.Marca);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoVeiculoNaoExiste()
    {
        var (service, _, _) = CriarContexto();

        var resultado = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DeveListarVeiculos()
    {
        var (service, _, cliente) = CriarContexto();

        await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);
        await service.CreateAsync(cliente.Id, "BRA2E19", "Fiat", "Uno", 2020);

        var resultado = await service.ListAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, veiculo => veiculo.Placa == "ABC1234");
        Assert.Contains(resultado, veiculo => veiculo.Placa == "BRA2E19");
    }

    [Fact]
    public async Task DeveRetornarListaVazia()
    {
        var (service, _, _) = CriarContexto();

        var resultado = await service.ListAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task DeveAtualizarVeiculo()
    {
        var (service, _, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);

        var atualizado = await service.UpdateAsync(veiculoId, "BRA2E19", "Fiat", "Uno", 2020);
        var veiculo = await service.GetByIdAsync(veiculoId);

        Assert.True(atualizado);
        Assert.NotNull(veiculo);
        Assert.Equal("BRA2E19", veiculo.Placa);
        Assert.Equal("Fiat", veiculo.Marca);
        Assert.Equal("Uno", veiculo.Modelo);
        Assert.Equal(2020, veiculo.Ano);
        Assert.Equal(cliente.Id, veiculo.ClienteId);
    }

    [Fact]
    public async Task DeveAtualizarSemAlterarPlaca()
    {
        var (service, _, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC-1234", "Ford", "Ka", 2019);

        var atualizado = await service.UpdateAsync(veiculoId, "abc1234", "Ford", "Fiesta", 2020);

        Assert.True(atualizado);
        Assert.Equal("Fiesta", (await service.GetByIdAsync(veiculoId))!.Modelo);
    }

    [Fact]
    public async Task NaoDeveAtualizarComPlacaDuplicada()
    {
        var (service, _, cliente) = CriarContexto();

        await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);
        var segundoId = await service.CreateAsync(cliente.Id, "BRA2E19", "Fiat", "Uno", 2020);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(segundoId, "ABC-1234", "Fiat", "Uno", 2020));

        Assert.Equal("BRA2E19", (await service.GetByIdAsync(segundoId))!.Placa);
    }

    [Fact]
    public async Task NaoDeveAtualizarVeiculoInexistente()
    {
        var (service, _, _) = CriarContexto();

        var atualizado = await service.UpdateAsync(Guid.NewGuid(), "ABC1234", "Ford", "Ka", 2019);

        Assert.False(atualizado);
    }

    [Fact]
    public async Task NaoDeveAlterarDadosComAnoInvalido()
    {
        var (service, _, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(veiculoId, "BRA2E19", "Fiat", "Uno", 0));

        var veiculo = await service.GetByIdAsync(veiculoId);

        Assert.Equal("ABC1234", veiculo!.Placa);
        Assert.Equal("Ford", veiculo.Marca);
        Assert.Equal(2019, veiculo.Ano);
    }

    [Fact]
    public async Task DeveExcluirVeiculoSemDependencias()
    {
        var (service, repositorio, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);

        var excluido = await service.DeleteAsync(veiculoId);

        Assert.True(excluido);
        Assert.Null(await service.GetByIdAsync(veiculoId));
        Assert.Empty(repositorio.Veiculos);
    }

    [Fact]
    public async Task NaoDeveExcluirVeiculoInexistente()
    {
        var (service, _, _) = CriarContexto();

        var excluido = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(excluido);
    }

    [Fact]
    public async Task NaoDeveExcluirVeiculoComOrdemServico()
    {
        var (service, repositorio, cliente) = CriarContexto();
        var veiculoId = await service.CreateAsync(cliente.Id, "ABC1234", "Ford", "Ka", 2019);

        repositorio.VeiculosComDependencias.Add(veiculoId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(veiculoId));

        Assert.Single(repositorio.Veiculos);
    }

    [Fact]
    public async Task NaoDeveAceitarIdentificadorVazio()
    {
        var (service, _, _) = CriarContexto();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(Guid.Empty, "ABC1234", "Ford", "Ka", 2019));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteAsync(Guid.Empty));
    }


    private static (VeiculoApplicationService Service, FakeVeiculoRepository Repositorio, Cliente Cliente) CriarContexto()
    {
        var cliente = new Cliente("João Silva", new Documento("52998224725"));
        var repositorioClientes = new FakeClienteRepository(cliente);
        var repositorioVeiculos = new FakeVeiculoRepository();

        var service = new VeiculoApplicationService(repositorioVeiculos, repositorioClientes);

        return (service, repositorioVeiculos, cliente);
    }

    private sealed class FakeVeiculoRepository : IVeiculoRepository
    {
        private readonly List<Veiculo> _veiculos = new();

        public IReadOnlyList<Veiculo> Veiculos => _veiculos.AsReadOnly();
        public HashSet<Guid> VeiculosComDependencias { get; } = new();

        public Task<bool> HasPlacaAsync(Placa placa, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_veiculos.Any(veiculo => veiculo.Placa == placa));
        }

        public Task CreateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
        {
            _veiculos.Add(veiculo);
            return Task.CompletedTask;
        }

        public Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_veiculos.FirstOrDefault(veiculo => veiculo.Id == id));
        }

        public Task<IReadOnlyList<Veiculo>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Veiculo>>(_veiculos.ToList());
        }

        public Task UpdateAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Veiculo veiculo, CancellationToken cancellationToken = default)
        {
            _veiculos.Remove(veiculo);
            return Task.CompletedTask;
        }

        public Task<bool> HasDependenciesAsync(Guid veiculoId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VeiculosComDependencias.Contains(veiculoId));
        }
    }

    private sealed class FakeClienteRepository(Cliente cliente) : IClienteRepository
    {
        public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Cliente? resultado = cliente.Id == id ? cliente : null;
            return Task.FromResult(resultado);
        }

        public Task<bool> HasDocumentoAsync(Documento documento, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CreateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HasDependenciesAsync(Guid clienteId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

}
