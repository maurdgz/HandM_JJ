using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImportNet.Infrastructure.Persistence;

internal sealed class MovimientoAjusteConfiguration
    : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(
        EntityTypeBuilder<MovimientoInventario> entity)
    {
        entity.Ignore(x => x.CantidadConSigno);

        entity.Property(x => x.Observacion)
            .HasMaxLength(AjusteInventario.ObservacionMaxima);

        // Solo los ajustes llevan motivo, y todo ajuste debe tenerlo.
        entity.ToTable(t => t.HasCheckConstraint(
            "CK_Movimiento_MotivoAjuste",
            "(\"Tipo\" = 2 AND \"MotivoAjuste\" IN (0, 1, 2, 3)) OR (\"Tipo\" <> 2 AND \"MotivoAjuste\" IS NULL)"));

        // Apoya el kardex: movimientos de un producto ordenados por fecha.
        entity.HasIndex(x => new { x.ProductoId, x.Fecha })
            .HasDatabaseName("IX_Movimiento_ProductoFecha");
    }
}
