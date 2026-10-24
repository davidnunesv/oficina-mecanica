
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class PecaInsumoConfiguration : IEntityTypeConfiguration<PecaInsumo>
{
    public void Configure(EntityTypeBuilder<PecaInsumo> builder)
    {
        builder.ToTable("PecasInsumos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<OrdemServico>()
            .WithMany()
            .HasForeignKey(p => p.OrdemServicoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
