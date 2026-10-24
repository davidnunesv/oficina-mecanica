
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class ItemPecaOSConfiguration : IEntityTypeConfiguration<ItemPecaOS>
{
    public void Configure(EntityTypeBuilder<ItemPecaOS> builder)
    {
        builder.ToTable("ItensPecaOS");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property<Guid>("OrdemServicoId").IsRequired();

        builder.Property(item => item.PrecoAplicado).HasPrecision(18, 2).IsRequired();

        builder.HasOne<PecaInsumo>()
            .WithMany()
            .HasForeignKey(item => item.PecaInsumoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex("OrdemServicoId", nameof(ItemPecaOS.PecaInsumoId))
            .IsUnique();
    }
}
