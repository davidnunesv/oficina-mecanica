
namespace Oficina.Domain.Entities;

public class ItemPecaOS
{
    public Guid Id { get; private set; }
    public Guid PecaInsumoId { get; private set; }
    public decimal PrecoAplicado { get; private set; }

    internal ItemPecaOS(Guid pecaInsumoId, decimal precoAplicado)
    {
        if (pecaInsumoId == Guid.Empty)
            throw new ArgumentException("A peça ou insumo é obrigatória.", nameof(pecaInsumoId));

        if (precoAplicado <= 0)
            throw new ArgumentException("O preço aplicado deve ser maior que zero.", nameof(precoAplicado));

        Id = Guid.NewGuid();
        PecaInsumoId = pecaInsumoId;
        PrecoAplicado = precoAplicado;
    }
}
