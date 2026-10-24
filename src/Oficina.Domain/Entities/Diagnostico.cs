
namespace Oficina.Domain.Entities;

public class Diagnostico
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; }
    public DateTimeOffset DataRegistro { get; private set; }

    internal Diagnostico(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição do diagnóstico é obrigatória.", nameof(descricao));

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        DataRegistro = DateTimeOffset.UtcNow;
    }
}
