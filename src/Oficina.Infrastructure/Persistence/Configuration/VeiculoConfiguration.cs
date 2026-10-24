
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;
using Oficina.Domain.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("Veiculos");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Placa)
            .HasConversion(placa => placa.Valor, valor => new Placa(valor))
            .HasMaxLength(7)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(v => v.Placa).IsUnique();

        builder.Property(v => v.Marca).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Ano).IsRequired();

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
