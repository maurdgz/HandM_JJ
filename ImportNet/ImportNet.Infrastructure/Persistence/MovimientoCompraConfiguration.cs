using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportNet.Infrastructure.Persistence;

internal sealed class MovimientoCompraConfiguration
    : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(
        EntityTypeBuilder<MovimientoInventario> entity)
    {
        entity.HasOne<Compra>()
            .WithMany()
            .HasForeignKey(x => x.CompraId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.CompraId)
            .HasDatabaseName("IX_MovimientoInventario_CompraId");
    }
}