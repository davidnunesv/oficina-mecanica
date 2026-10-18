
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

        var dados = ValidateData(placa, marca, modelo, ano);

        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Placa = dados.Placa;
        Marca = dados.Marca;
        Modelo = dados.Modelo;
        Ano = dados.Ano;
    }

    public void Update(Placa placa, string marca, string modelo, int ano)
    {
        var dados = ValidateData(placa, marca, modelo, ano);

        Placa = dados.Placa;
        Marca = dados.Marca;
        Modelo = dados.Modelo;
        Ano = dados.Ano;
    }

    private static (Placa Placa, string Marca, string Modelo, int Ano) ValidateData(Placa placa, string marca, string modelo, int ano)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (string.IsNullOrWhiteSpace(marca))
            throw new ArgumentException("A marca é obrigatória.", nameof(marca));

        if (string.IsNullOrWhiteSpace(modelo))
            throw new ArgumentException("O modelo é obrigatório.", nameof(modelo));

        if (ano <= 0)
            throw new ArgumentException("O ano deve ser maior que zero.", nameof(ano));

        return (placa, marca.Trim(), modelo.Trim(), ano);
    }
}
