
namespace Oficina.Domain.Entities;

public class Servico
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public decimal Preco { get; private set; }

    public Servico(string nome, string descricao, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do serviço é obrigatório.", nameof(nome));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição do serviço é obrigatória.", nameof(descricao));

        if (preco <= 0)
            throw new ArgumentException("O preço do serviço deve ser maior que zero.", nameof(preco));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Descricao = descricao.Trim();
        Preco = preco;
    }
}
