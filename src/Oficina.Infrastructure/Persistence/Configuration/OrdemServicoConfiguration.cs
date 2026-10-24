
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations;

public class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> builder)
    {
        builder.ToTable("OrdensServico");

        builder.HasKey(os => os.Id);
        builder.Property(os => os.Id).ValueGeneratedNever();

        builder.Property(os => os.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(os => os.DataInicioExecucao);
        builder.Property(os => os.DataFinalizacao);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(os => os.ClienteId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Veiculo>()
            .WithMany()
            .HasForeignKey(os => os.VeiculoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(os => os.Diagnostico)
            .WithOne()
            .HasForeignKey<Diagnostico>("OrdemServicoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(os => os.Orcamento)
            .WithOne()
            .HasForeignKey<Orcamento>("OrdemServicoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(os => os.ItensServico)
            .WithOne()
            .HasForeignKey("OrdemServicoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(os => os.ItensServico)
            .HasField("_itensServico")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(os => os.ItensPeca)
            .WithOne()
            .HasForeignKey("OrdemServicoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(os => os.ItensPeca)
            .HasField("_itensPeca")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
