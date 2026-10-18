using System;
using System.Linq;

namespace Oficina.Domain.ValueObjects
{
    public enum TipoDocumento
    {
        Cpf,
        Cnpj
    }


    public sealed record Documento
    {
        public string Valor { get; }
        public TipoDocumento Tipo { get; }

        public Documento(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("O documento é obrigatório.",nameof(valor));

            var numero = valor.Trim()
                .Replace(".", "")
                .Replace("-", "")
                .Replace("/", "")
                .ToUpperInvariant();

            if (numero.Length == 11)
            {
                if (!ValidarCpf(numero))
                    throw new ArgumentException("CPF inválido.", nameof(valor));

                Tipo = TipoDocumento.Cpf;
            }
            else if (numero.Length == 14)
            {
                if (!ValidarCnpj(numero))
                    throw new ArgumentException("CNPJ inválido.", nameof(valor));

                Tipo = TipoDocumento.Cnpj;
            }
            else
            {
                throw new ArgumentException("O documento deve ter 11 ou 14 caracteres.",nameof(valor));
            }

            Valor = numero;
        }

        private static bool ValidarCpf(string numero)
        {
            if (!numero.All(EhDigito))
                return false;

            if (numero.All(c => c == numero[0]))
                return false;

            int primeiro = CalcularDigito(
                numero[..9],
                [10, 9, 8, 7, 6, 5, 4, 3, 2]);

            int segundo = CalcularDigito(
                numero[..10],
                [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

            return primeiro == numero[9] - '0'
                && segundo == numero[10] - '0';
        }

        private static bool ValidarCnpj(string numero)
        {
            // As primeiras 12 posições aceitam letras e números.
            if (!numero[..12].All(EhAlfanumerico))
                return false;

            // Os dois dígitos verificadores devem ser numéricos.
            if (!numero[12..].All(EhDigito))
                return false;

            if (numero.All(c => c == numero[0]))
                return false;

            int primeiro = CalcularDigito(
                numero[..12],
                [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

            int segundo = CalcularDigito(
                numero[..13],
                [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

            return primeiro == numero[12] - '0'
                && segundo == numero[13] - '0';
        }

        private static int CalcularDigito(
            string baseDocumento,
            int[] pesos)
        {
            int soma = 0;

            for (int i = 0; i < pesos.Length; i++)
            {
                // Em ASCII, '0' equivale a 48 e 'A' a 65.
                // Portanto, 'A' - '0' resulta em 17.
                int valor = baseDocumento[i] - '0';

                soma += valor * pesos[i];
            }

            int resto = soma % 11;

            return resto < 2 ? 0 : 11 - resto;
        }

        private static bool EhDigito(char c)
            => c >= '0' && c <= '9';

        private static bool EhAlfanumerico(char c)
            => EhDigito(c) || (c >= 'A' && c <= 'Z');
    }
}
