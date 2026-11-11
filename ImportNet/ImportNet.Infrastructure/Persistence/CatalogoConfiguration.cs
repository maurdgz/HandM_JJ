using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ImportNet.Infrastructure.Persistence;

internal sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> e)
    {
        e.ToTable("Categoria");
        e.HasKey(x => x.Id);
        e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        e.Property(x => x.NombreNormalizado).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.NombreNormalizado).IsUnique();
    }
}
internal sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> e)
    {
        e.ToTable("Producto", t => {
            t.HasCheckConstraint("CK_Producto_Peso", "\"Peso\" IS NULL OR \"Peso\" > 0");
            t.HasCheckConstraint("CK_Producto_Precio", "\"PrecioVenta\" >= 0");
            t.HasCheckConstraint("CK_Producto_Costo", "\"CostoPromedio\" >= 0");
            t.HasCheckConstraint("CK_Producto_Importacion", "(\"CostoImportacionUsd\" IS NULL AND \"TipoCambioImportacion\" IS NULL) OR (\"CostoImportacionUsd\" IS NOT NULL AND \"TipoCambioImportacion\" IS NOT NULL AND \"CostoImportacionUsd\" >= 0 AND \"TipoCambioImportacion\" > 0)");
            t.HasCheckConstraint("CK_Producto_StockMinimo", "\"StockMinimo\" >= 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        e.Property(x => x.Descripcion).HasMaxLength(1000);
        e.Property(x => x.UnidadMedida).HasMaxLength(30).IsRequired();
        e.Property(x => x.PartidaArancelaria).HasMaxLength(20);
        e.Property(x => x.PrecioVenta).HasPrecision(18, 2);
        e.Property(x => x.CostoPromedio).HasPrecision(18, 4);
        e.Property(x => x.CostoImportacionUsd).HasPrecision(18, 2);
        e.Property(x => x.TipoCambioImportacion).HasPrecision(18, 6);
        e.HasIndex(x => x.Codigo).IsUnique();
        e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}
