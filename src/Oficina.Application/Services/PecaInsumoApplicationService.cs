
using Oficina.Application.DTOs.PecasInsumos;
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.Application.Services;

public class PecaInsumoApplicationService(IPecaInsumoRepository pecaRepository)
{
    private readonly IPecaInsumoRepository _pecaRepository = pecaRepository ?? throw new ArgumentNullException(nameof(pecaRepository));

    public async Task<Guid> CreateAsync(string nome, decimal preco, CancellationToken cancellationToken = default)
    {
        var peca = new PecaInsumo(nome, preco);

        await _pecaRepository.CreateAsync(peca, cancellationToken);

        return peca.Id;
    }

    public async Task<PecaInsumoDto?> GetByIdAsync(Guid pecaId, CancellationToken cancellationToken = default)
    {
        if (pecaId == Guid.Empty)
            throw new ArgumentException("O ID da peça é obrigatório.", nameof(pecaId));

        var peca = await _pecaRepository.GetByIdAsync(pecaId, cancellationToken);

        return peca is null ? null : MapToDto(peca);
    }

    public async Task<IReadOnlyList<PecaInsumoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var pecas = await _pecaRepository.ListAsync(cancellationToken);

        return pecas.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateAsync(Guid pecaId, string nome, decimal preco, CancellationToken cancellationToken = default)
    {
        if (pecaId == Guid.Empty)
            throw new ArgumentException("O ID da peça é obrigatório.", nameof(pecaId));

        var peca = await _pecaRepository.GetByIdAsync(pecaId, cancellationToken);

        if (peca is null)
            return false;

        peca.Update(nome, preco);

        await _pecaRepository.UpdateAsync(peca, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid pecaId, CancellationToken cancellationToken = default)
    {
        if (pecaId == Guid.Empty)
            throw new ArgumentException("O ID da peça é obrigatório.", nameof(pecaId));

        var peca = await _pecaRepository.GetByIdAsync(pecaId, cancellationToken);

        if (peca is null)
            return false;

        if (peca.Status != StatusPecaInsumo.Disponivel)
            throw new InvalidOperationException("Somente peças disponíveis podem ser excluídas.");

        if (await _pecaRepository.HasDependenciesAsync(pecaId, cancellationToken))
            throw new InvalidOperationException("Não é possível excluir uma peça vinculada a uma Ordem de Serviço.");

        await _pecaRepository.DeleteAsync(peca, cancellationToken);

        return true;
    }

    private static PecaInsumoDto MapToDto(PecaInsumo peca)
    {
        return new PecaInsumoDto(peca.Id, peca.Nome, peca.Preco, peca.Status.ToString(), peca.OrdemServicoId);
    }
}
