using Oficina.Domain.Enums;

namespace Oficina.Domain.Entities;

public class Orcamento
{
    public Guid Id { get; private set; }
    public decimal ValorServicos { get; private set; }
    public decimal ValorPecas { get; private set; }
    public decimal ValorTotal => ValorServicos + ValorPecas;
    public StatusOrcamento Status { get; private set; }

    internal Orcamento(decimal valorServicos, decimal valorPecas)
    {
        if (valorServicos < 0)
            throw new ArgumentException("O valor dos serviços não pode ser negativo.", nameof(valorServicos));

        if (valorPecas < 0)
            throw new ArgumentException("O valor das peças não pode ser negativo.", nameof(valorPecas));

        if (valorServicos + valorPecas <= 0)
            throw new ArgumentException("O orçamento deve possuir valor maior que zero.");

        Id = Guid.NewGuid();
        ValorServicos = valorServicos;
        ValorPecas = valorPecas;
        Status = StatusOrcamento.Pendente;
    }


    internal void Aprovar()
    {
        if (Status != StatusOrcamento.Pendente)
            throw new InvalidOperationException("Somente um orçamento pendente pode ser aprovado.");

        Status = StatusOrcamento.Aprovado;
    }

    internal void Rejeitar()
    {
        if (Status != StatusOrcamento.Pendente)
            throw new InvalidOperationException("Somente um orçamento pendente pode ser rejeitado.");

        Status = StatusOrcamento.Rejeitado;
    }

}