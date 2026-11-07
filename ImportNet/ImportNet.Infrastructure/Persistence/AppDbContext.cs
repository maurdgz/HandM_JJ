using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<string>().HaveMaxLength(200);
        builder.Properties<decimal>().HavePrecision(18, 3);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        b.Entity<Empleado>(e => {
            e.ToTable("Empleado");
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Empleado>(x => x.UsuarioId);
        });
        b.Entity<Almacen>().ToTable("Almacen");
        b.Entity<Inventario>(e => {
            e.ToTable("Inventario", t => t.HasCheckConstraint("CK_Inventario_Cantidad", "\"Cantidad\" >= 0"));
            e.HasIndex(x => new { x.ProductoId, x.AlmacenId }).IsUnique();
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
            e.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId);
            if (Database.IsNpgsql()) e.Property(x => x.Version).IsRowVersion();
            else e.Ignore(x => x.Version);
        });
        b.Entity<Proveedor>(e => {
            e.ToTable("Proveedor", t => t.HasCheckConstraint("CK_Proveedor_Tipo", "\"Tipo\" IN (0, 1)"));
            e.Property(x => x.Pais).HasMaxLength(100);
        });
        b.Entity<ProveedorProducto>(e => {
            e.ToTable("ProveedorProducto");
            e.HasKey(x => new { x.ProveedorId, x.ProductoId });
            e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId);
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
        });
        b.Entity<Cliente>(e => {
            e.ToTable("Cliente");
            e.Property(x => x.MonedaPreferida).HasMaxLength(3);
            e.Property(x => x.IncotermPreferido).HasMaxLength(3);
        });
        b.Entity<OrdenVenta>(e => {
            e.ToTable("OrdenVenta", t => {
                t.HasCheckConstraint("CK_OrdenVenta_Importes", "\"TipoCambio\" > 0 AND \"Subtotal\" >= 0 AND \"Total\" = \"Subtotal\"");
                t.HasCheckConstraint("CK_OrdenVenta_Estado", "\"Estado\" BETWEEN 0 AND 4");
            });
            e.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId);
            e.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId);
            e.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId);
            e.Property(x => x.Moneda).HasMaxLength(3);
            e.Property(x => x.Incoterm).HasMaxLength(3);
            e.Property(x => x.TipoCambio).HasPrecision(18, 6);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasIndex(x => x.Fecha);
            if (Database.IsNpgsql()) e.Property(x => x.Version).IsRowVersion();
            else e.Ignore(x => x.Version);
        });
        b.Entity<DetalleOrdenVenta>(e => {
            e.ToTable("DetalleOrdenVenta", t => t.HasCheckConstraint("CK_Detalle_Importes", "\"Cantidad\" > 0 AND \"PrecioUnitario\" >= 0 AND \"Subtotal\" >= 0"));
            e.HasOne<OrdenVenta>().WithMany().HasForeignKey(x => x.OrdenVentaId);
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
            e.HasIndex(x => new { x.OrdenVentaId, x.ProductoId }).IsUnique();
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
        });
        b.Entity<MovimientoInventario>(e => {
            e.ToTable("MovimientoInventario", t => {
                t.HasCheckConstraint("CK_Movimiento_Cantidad", "(\"Tipo\" IN (0, 1) AND \"Cantidad\" > 0) OR (\"Tipo\" = 2 AND \"Cantidad\" <> 0)");
            });
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
            e.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId);
            e.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId);
            e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId);
            e.HasOne<OrdenVenta>().WithMany().HasForeignKey(x => x.OrdenVentaId);
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.OrdenVentaId, x.ProductoId, x.AlmacenId, x.Tipo }).IsUnique();
        });
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ValidarInmutabilidad();
        return base.SaveChangesAsync(ct);
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidarInmutabilidad();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    private void ValidarInmutabilidad()
    {
        if (ChangeTracker.Entries<MovimientoInventario>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new ReglaNegocioException("Los movimientos son inmutables; registre un ajuste.");
    }
}
