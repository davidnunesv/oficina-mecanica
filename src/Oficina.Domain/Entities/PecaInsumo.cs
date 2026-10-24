
using Oficina.Domain.Enums;

namespace Oficina.Domain.Entities;

public class PecaInsumo
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public StatusPecaInsumo Status { get; private set; }
    public Guid? OrdemServicoId { get; private set; }

    public PecaInsumo(string nome, decimal preco)
    {
        var dados = ValidateData(nome, preco);

        Id = Guid.NewGuid();
        Nome = dados.Nome;
        Preco = dados.Preco;
        Status = StatusPecaInsumo.Disponivel;
    }

    public void Update(string nome, decimal preco)
    {
        if (Status != StatusPecaInsumo.Disponivel)
            throw new InvalidOperationException("Somente peças disponíveis podem ser atualizadas.");

        var dados = ValidateData(nome, preco);

        Nome = dados.Nome;
        Preco = dados.Preco;
    }

    public void Reservar(Guid ordemServicoId)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("A Ordem de Serviço é obrigatória.", nameof(ordemServicoId));

        if (Status != StatusPecaInsumo.Disponivel)
            throw new InvalidOperationException("A peça não está disponível para reserva.");

        OrdemServicoId = ordemServicoId;
        Status = StatusPecaInsumo.Reservada;
    }

    public void Consumir(Guid ordemServicoId)
    {
        if (ordemServicoId == Guid.Empty)
            throw new ArgumentException("A Ordem de Serviço é obrigatória.", nameof(ordemServicoId));

        if (Status != StatusPecaInsumo.Reservada)
            throw new InvalidOperationException("A peça precisa estar reservada para ser consumida.");

        if (OrdemServicoId != ordemServicoId)
            throw new InvalidOperationException("A peça está reservada para outra Ordem de Serviço.");

        Status = StatusPecaInsumo.Consumida;
    }

    private static (string Nome, decimal Preco) ValidateData(string nome, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome da peça é obrigatório.", nameof(nome));

        if (preco <= 0)
            throw new ArgumentException("O preço da peça deve ser maior que zero.", nameof(preco));

        return (nome.Trim(), preco);
    }
}
