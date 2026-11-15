using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ImportNet.Infrastructure.Persistence;
internal sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> e)
    {
        e.ToTable("Compra", t => {
            t.HasCheckConstraint("CK_Compra_Estado", "\"Estado\" IN (0, 1)");
            t.HasCheckConstraint("CK_Compra_Importes", "\"TipoCambio\" > 0 AND \"FletePen\" >= 0 AND \"AduanaPen\" >= 0");
        });
        e.HasKey(x => x.Id); e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId);
        e.Property(x => x.TipoCambio).HasPrecision(18, 6);
        e.Property(x => x.FletePen).HasPrecision(18, 2); e.Property(x => x.AduanaPen).HasPrecision(18, 2);
        e.HasIndex(x => new { x.Fecha, x.Id });
        e.HasMany(x => x.Lineas).WithOne().HasForeignKey(x => x.CompraId).IsRequired();
        e.Navigation(x => x.Lineas).HasField("_lineas").UsePropertyAccessMode(PropertyAccessMode.Field);
        e.Ignore(x => x.SubtotalUsd); e.Ignore(x => x.MercaderiaPen); e.Ignore(x => x.TotalPen);
    }
}
internal sealed class DetalleCompraConfiguration : IEntityTypeConfiguration<DetalleCompra>
{
    public void Configure(EntityTypeBuilder<DetalleCompra> e)
    {
        e.ToTable("DetalleCompra", t => t.HasCheckConstraint("CK_DetalleCompra_Importes", "\"Cantidad\" > 0 AND \"CostoUnitarioUsd\" > 0 AND \"MercaderiaPen\" >= 0 AND \"FletePen\" >= 0 AND \"AduanaPen\" >= 0"));
        e.HasKey(x => x.Id); e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
        e.HasIndex(x => x.CompraId);
        e.Property(x => x.Cantidad).HasPrecision(18, 3); e.Property(x => x.CostoUnitarioUsd).HasPrecision(18, 4);
        e.Property(x => x.MercaderiaPen).HasPrecision(18, 2); e.Property(x => x.FletePen).HasPrecision(18, 2); e.Property(x => x.AduanaPen).HasPrecision(18, 2);
        e.Ignore(x => x.SubtotalUsd); e.Ignore(x => x.TotalPen); e.Ignore(x => x.CostoUnitarioPuestoPen);
    }
}
