using Oficina.Domain.ValueObjects;

namespace Oficina.Domain.Entities;

public class Veiculo
{
    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public Placa Placa { get; private set; }

    public string Marca { get; private set; }
    public string Modelo { get; private set; }
    public int Ano { get; private set; }

    public Veiculo(Guid clienteId, Placa placa, string marca, string modelo, int ano)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("O cliente é obrigatório.", nameof(clienteId));

        ArgumentNullException.ThrowIfNull(placa);

        if (string.IsNullOrWhiteSpace(marca))
            throw new ArgumentException("A marca é obrigatória.", nameof(marca));

        if (string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("O modelo é obrigatório.", nameof(modelo));

        if (ano <= 0)
            throw new ArgumentException("O ano deve ser maior que zero.", nameof(ano));

        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Placa = placa;
        Marca = marca.Trim();
        Modelo = modelo.Trim();
        Ano = ano;
    }
}
