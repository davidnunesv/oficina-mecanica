using Oficina.Domain.ValueObjects;

namespace Oficina.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Documento Documento { get; private set; }

    public Cliente(string nome, Documento documento)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(nome));

        ArgumentNullException.ThrowIfNull(documento);

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Documento = documento;
    }


    public void Update(string nome, Documento documento)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(nome));

        ArgumentNullException.ThrowIfNull(documento);

        Nome = nome.Trim();
        Documento = documento;
    }
}
