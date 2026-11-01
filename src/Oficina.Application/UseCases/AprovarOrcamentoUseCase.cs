
using Oficina.Application.Interfaces;
using Oficina.Domain.Entities;
using Oficina.Domain.Enums;

namespace Oficina.Application.UseCases.OrdensServico;

public class AprovarOrcamentoUseCase(
    IOrdemServicoRepository ordemServicoRepository,
    IPecaInsumoRepository pecaInsumoRepository,
    ITransactionManager transactionManager)
{
    private readonly IOrdemServicoRepository _ordemServicoRepository = ordemServicoRepository ?? throw new ArgumentNullException(nameof(ordemServicoRepository));
    private readonly IPecaInsumoRepository _pecaInsumoRepository = pecaInsumoRepository ?? throw new ArgumentNullException(nameof(pecaInsumoRepository));
    private readonly ITransactionManager _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));

    public Task<bool> ExecuteAsync(Guid ordemServicoId, Guid ClienteId, CancellationToken cancellationToken = default)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("O ID da Ordem de Serviço é obrigatório.", nameof(ordemServicoId));

        return _transactionManager.ExecuteInTransactionAsync(async ct =>
        {
            var os = await _ordemServicoRepository.GetByIdAsync(ordemServicoId, ct);

            if (os is null || os.ClienteId != ClienteId)
                return false;

            if (os.Status != StatusOS.AguardandoAprovacao || os.Orcamento?.Status != StatusOrcamento.Pendente)
                throw new InvalidOperationException("A OS precisa possuir um orçamento pendente para ser aprovada.");

            var pecas = new List<PecaInsumo>();

            foreach (var item in os.ItensPeca)
            {
                var peca = await _pecaInsumoRepository.GetByIdAsync(item.PecaInsumoId, ct);

                if (peca is null)
                    throw new KeyNotFoundException($"A peça {item.PecaInsumoId} não foi encontrada.");

                if (peca.Status != StatusPecaInsumo.Disponivel)
                    throw new InvalidOperationException($"A peça {peca.Id} não está disponível para reserva.");

                pecas.Add(peca);
            }

            foreach (var peca in pecas)
            {
                peca.Reservar(os.Id);
                await _pecaInsumoRepository.UpdateAsync(peca, ct);
            }

            os.AprovarOrcamento();
            os.IniciarExecucao();

            await _ordemServicoRepository.UpdateAsync(os, ct);

            return true;
        }, cancellationToken);
    }
}
