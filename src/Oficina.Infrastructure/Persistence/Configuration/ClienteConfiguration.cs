
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Documento)
            .HasConversion(documento => documento.Valor, valor => new Documento(valor))
            .HasMaxLength(14)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(c => c.Documento).IsUnique();
    }
}
