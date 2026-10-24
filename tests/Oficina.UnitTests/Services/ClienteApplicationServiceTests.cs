
using Oficina.Application.Interfaces;
using Oficina.Application.Services;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.UnitTests.Services;

public class ClienteApplicationServiceTests
{
    [Fact]
    public async Task DeveCadastrarClienteComCpfValido()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "529.982.247-25");

        var cliente = Assert.Single(repositorio.Clientes);

        Assert.Equal(clienteId, cliente.Id);
        Assert.Equal("João Silva", cliente.Nome);
        Assert.Equal("52998224725", cliente.Documento.Valor);
    }

    [Fact]
    public async Task DeveCadastrarClienteComCnpjAlfanumerico()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await service.CreateAsync("Oficina ABC", "12.ABC.345/01DE-35");

        var cliente = Assert.Single(repositorio.Clientes);

        Assert.Equal("12ABC34501DE35", cliente.Documento.Valor);
    }

    [Fact]
    public async Task NaoDeveCadastrarDocumentoDuplicado()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await service.CreateAsync("João Silva", "529.982.247-25");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("Outro Cliente", "52998224725"));

        Assert.Single(repositorio.Clientes);
    }

    [Fact]
    public async Task NaoDeveCadastrarDocumentoInvalido()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("João Silva", "123"));

        Assert.Empty(repositorio.Clientes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NaoDeveCadastrarClienteComNomeVazio(string nome)
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(nome, "52998224725"));

        Assert.Empty(repositorio.Clientes);
    }

    [Fact]
    public async Task DeveConsultarClienteExistente()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "529.982.247-25");

        var resultado = await service.GetByIdAsync(clienteId);

        Assert.NotNull(resultado);
        Assert.Equal(clienteId, resultado.Id);
        Assert.Equal("João Silva", resultado.Nome);
        Assert.Equal("52998224725", resultado.Documento);
        Assert.Equal("Cpf", resultado.TipoDocumento);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoClienteNaoExiste()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var resultado = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task NaoDeveConsultarComIdVazio()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
    }


    [Fact]
    public async Task DeveListarClientesCadastrados()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await service.CreateAsync("David Nunes", "52998224725");
        await service.CreateAsync("Oficina X", "12ABC34501DE35");

        var resultado = await service.ListAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, cliente => cliente.Nome == "David Nunes");
        Assert.Contains(resultado, cliente => cliente.Nome == "Oficina X");
    }

    [Fact]
    public async Task DeveRetornarListaVaziaQuandoNaoExistiremClientes()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var resultado = await service.ListAsync();

        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task DeveListarClientesComDocumentosNormalizados()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        await service.CreateAsync("David Nunes", "529.982.247-25");
        await service.CreateAsync("Oficina X", "12.ABC.345/01DE-35");

        var resultado = await service.ListAsync();

        Assert.Contains(resultado, cliente => cliente.Documento == "52998224725" && cliente.TipoDocumento == "Cpf");
        Assert.Contains(resultado, cliente => cliente.Documento == "12ABC34501DE35" && cliente.TipoDocumento == "Cnpj");
    }



    [Fact]
    public async Task DeveAtualizarClienteExistente()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        var atualizado = await service.UpdateAsync(clienteId, "João Oliveira", "12ABC34501DE35");

        var cliente = await service.GetByIdAsync(clienteId);

        Assert.True(atualizado);
        Assert.NotNull(cliente);
        Assert.Equal("João Oliveira", cliente.Nome);
        Assert.Equal("12ABC34501DE35", cliente.Documento);
        Assert.Equal("Cnpj", cliente.TipoDocumento);
    }

    [Fact]
    public async Task DevePermitirAtualizarNomeSemAlterarDocumento()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        var atualizado = await service.UpdateAsync(clienteId, "João Oliveira", "529.982.247-25");

        var cliente = await service.GetByIdAsync(clienteId);

        Assert.True(atualizado);
        Assert.Equal("João Oliveira", cliente!.Nome);
        Assert.Equal("52998224725", cliente.Documento);
    }

    [Fact]
    public async Task NaoDeveAtualizarClienteInexistente()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var atualizado = await service.UpdateAsync(Guid.NewGuid(), "João Silva", "52998224725");

        Assert.False(atualizado);
    }

    [Fact]
    public async Task NaoDeveAtualizarClienteComDocumentoDuplicado()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var primeiroId = await service.CreateAsync("João Silva", "52998224725");
        var segundoId = await service.CreateAsync("Oficina ABC", "12ABC34501DE35");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(segundoId, "Oficina Atualizada", "52998224725"));

        var primeiro = await service.GetByIdAsync(primeiroId);
        var segundo = await service.GetByIdAsync(segundoId);

        Assert.Equal("52998224725", primeiro!.Documento);
        Assert.Equal("12ABC34501DE35", segundo!.Documento);
        Assert.Equal("Oficina ABC", segundo.Nome);
    }

    [Fact]
    public async Task NaoDeveAtualizarClienteComNomeVazio()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(clienteId, "", "12ABC34501DE35"));

        var cliente = await service.GetByIdAsync(clienteId);

        Assert.Equal("João Silva", cliente!.Nome);
        Assert.Equal("52998224725", cliente.Documento);
    }

    [Fact]
    public async Task NaoDeveAtualizarClienteComDocumentoInvalido()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(clienteId, "Novo Nome", "123"));

        var cliente = await service.GetByIdAsync(clienteId);

        Assert.Equal("João Silva", cliente!.Nome);
        Assert.Equal("52998224725", cliente.Documento);
    }



    [Fact]
    public async Task DeveExcluirClienteSemVinculos()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        var excluido = await service.DeleteAsync(clienteId);
        var cliente = await service.GetByIdAsync(clienteId);

        Assert.True(excluido);
        Assert.Null(cliente);
        Assert.Empty(repositorio.Clientes);
    }

    [Fact]
    public async Task NaoDeveExcluirClienteInexistente()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var excluido = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(excluido);
    }

    [Fact]
    public async Task NaoDeveExcluirClienteComVinculos()
    {
        var repositorio = new FakeClienteRepository();
        var service = new ClienteApplicationService(repositorio);

        var clienteId = await service.CreateAsync("João Silva", "52998224725");

        repositorio.ClientesComVinculos.Add(clienteId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(clienteId));

        Assert.Single(repositorio.Clientes);
        Assert.NotNull(await service.GetByIdAsync(clienteId));
    }



    private class FakeClienteRepository : IClienteRepository
    {
        private readonly List<Cliente> _clientes = new();

        public IReadOnlyList<Cliente> Clientes => _clientes.AsReadOnly();

        public Task<bool> HasDocumentoAsync(Documento documento, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_clientes.Any(cliente => cliente.Documento == documento));
        }

        public Task CreateAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _clientes.Add(cliente);
            return Task.CompletedTask;
        }

        public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_clientes.FirstOrDefault(cliente => cliente.Id == id));
        }

        public Task<IReadOnlyList<Cliente>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Cliente>>(_clientes.ToList());
        }


        public HashSet<Guid> ClientesComVinculos { get; } = new();

        public Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            // A entidade já está armazenada por referência na lista.
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default)
        {
            _clientes.Remove(cliente);
            return Task.CompletedTask;
        }

        public Task<bool> HasDependenciesAsync(Guid clienteId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ClientesComVinculos.Contains(clienteId));
        }

    }
}
