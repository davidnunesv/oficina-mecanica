namespace Oficina.Domain.Entities;

public class ItemServico
{
    public Guid Id { get; private set; }
    public Guid ServicoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoAplicado { get; private set; }

    internal ItemServico(Guid servicoId, int quantidade, decimal precoAplicado)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("O serviço é obrigatório.", nameof(servicoId));

        if (quantidade <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(quantidade));

        if (precoAplicado <= 0)
            throw new ArgumentException("O preço aplicado deve ser maior que zero.", nameof(precoAplicado));

        Id = Guid.NewGuid();
        ServicoId = servicoId;
        Quantidade = quantidade;
        PrecoAplicado = precoAplicado;
    }

    public decimal CalcularSubtotal()
    {
        return Quantidade * PrecoAplicado;
    }
}
