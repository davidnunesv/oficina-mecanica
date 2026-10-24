using System.Text.RegularExpressions;

namespace Oficina.Domain.ValueObjects;

public sealed record Placa
{
    public string Valor { get; }

    public Placa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("A placa é obrigatória.",nameof(valor));

        var placa = valor.Trim().ToUpperInvariant();

        // Padrão antigo: ABC-1234 ou ABC1234
        const string padraoAntigo =
            @"^[A-Z]{3}-?[0-9]{4}$";

        // Padrão Mercosul: ABC1D23
        const string padraoMercosul =
            @"^[A-Z]{3}[0-9][A-Z][0-9]{2}$";

        bool placaValida =
            Regex.IsMatch(placa, padraoAntigo) ||
            Regex.IsMatch(placa, padraoMercosul);

        if (!placaValida)
            throw new ArgumentException("A placa informada é inválida.",nameof(valor));

        Valor = placa.Replace("-", "");
    }
}
