
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class ItemServicoConfiguration : IEntityTypeConfiguration<ItemServico>
{
    public void Configure(EntityTypeBuilder<ItemServico> builder)
    {
        builder.ToTable("ItensServico");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property<Guid>("OrdemServicoId").IsRequired();

        builder.Property(item => item.Quantidade).IsRequired();
        builder.Property(item => item.PrecoAplicado).HasPrecision(18, 2).IsRequired();

        builder.HasOne<Servico>()
            .WithMany()
            .HasForeignKey(item => item.ServicoId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
