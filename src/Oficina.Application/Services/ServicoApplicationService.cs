
using Oficina.Application.DTOs.Servicos;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;

namespace Oficina.Application.Services;

public class ServicoApplicationService(IServicoRepository servicoRepository)
{
    private readonly IServicoRepository _servicoRepository = servicoRepository ?? throw new ArgumentNullException(nameof(servicoRepository));

    public async Task<Guid> CreateAsync(string nome, string descricao, decimal preco, CancellationToken cancellationToken = default)
    {
        var servico = new Servico(nome, descricao, preco);

        await _servicoRepository.CreateAsync(servico, cancellationToken);

        return servico.Id;
    }

    public async Task<ServicoDto?> GetByIdAsync(Guid servicoId, CancellationToken cancellationToken = default)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("O ID do serviço é obrigatório.", nameof(servicoId));

        var servico = await _servicoRepository.GetByIdAsync(servicoId, cancellationToken);

        return servico is null ? null : MapToDto(servico);
    }

    public async Task<IReadOnlyList<ServicoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var servicos = await _servicoRepository.ListAsync(cancellationToken);

        return servicos.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateAsync(Guid servicoId, string nome, string descricao, decimal preco, CancellationToken cancellationToken = default)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("O ID do serviço é obrigatório.", nameof(servicoId));

        var servico = await _servicoRepository.GetByIdAsync(servicoId, cancellationToken);

        if (servico is null)
            return false;

        servico.Update(nome, descricao, preco);

        await _servicoRepository.UpdateAsync(servico, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid servicoId, CancellationToken cancellationToken = default)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("O ID do serviço é obrigatório.", nameof(servicoId));

        var servico = await _servicoRepository.GetByIdAsync(servicoId, cancellationToken);

        if (servico is null)
            return false;

        if (await _servicoRepository.HasDependenciesAsync(servicoId, cancellationToken))
            throw new InvalidOperationException("Não é possível excluir um serviço vinculado a ordens de serviço.");

        await _servicoRepository.DeleteAsync(servico, cancellationToken);

        return true;
    }

    private static ServicoDto MapToDto(Servico servico)
    {
        return new ServicoDto(servico.Id, servico.Nome, servico.Descricao, servico.Preco);
    }
}
