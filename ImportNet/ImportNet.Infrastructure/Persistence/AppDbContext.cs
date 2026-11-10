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
            e.Property(x => x.Apellidos).HasMaxLength(100);
            e.Property(x => x.DocumentoIdentidad).HasMaxLength(20);
            e.HasIndex(x => x.DocumentoIdentidad).IsUnique();
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Empleado>(x => x.UsuarioId);
        });
        b.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
        b.Entity<Almacen>(e => {
            e.ToTable("Almacen");
            e.HasData(new Almacen { Id = Almacen.PrincipalId, Nombre = "Principal" });
        });
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
            e.ToTable("Cliente", t => t.HasCheckConstraint("CK_Cliente_TipoDocumento", "\"TipoDocumento\" IN (0, 1)"));
            e.Property(x => x.NumeroDocumento).HasMaxLength(11);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasIndex(x => x.NumeroDocumento).IsUnique();
        });
        b.Entity<Venta>(e => {
            e.ToTable("Venta", t => {
                t.HasCheckConstraint("CK_Venta_Importes", "\"Subtotal\" >= 0 AND \"Descuento\" >= 0 AND \"Descuento\" <= \"Subtotal\" AND \"Total\" = \"Subtotal\" - \"Descuento\"");
                t.HasCheckConstraint("CK_Venta_Estado", "\"Estado\" IN (0, 1)");
                t.HasCheckConstraint("CK_Venta_Anulacion", "(\"Estado\" = 0 AND \"MotivoAnulacion\" IS NULL) OR (\"Estado\" = 1 AND \"MotivoAnulacion\" IS NOT NULL)");
            });
            e.HasOne<Empleado>().WithMany().HasForeignKey(x => x.EmpleadoId);
            e.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Descuento).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.MotivoAnulacion).HasMaxLength(300);
            e.HasIndex(x => x.Numero).IsUnique();
            e.HasIndex(x => x.Fecha);
            if (Database.IsNpgsql()) e.Property(x => x.Version).IsRowVersion();
            else e.Ignore(x => x.Version);
        });
        b.Entity<DetalleVenta>(e => {
            e.ToTable("DetalleVenta", t => t.HasCheckConstraint("CK_DetalleVenta_Importes", "\"Cantidad\" > 0 AND \"PrecioUnitario\" >= 0 AND \"Descuento\" >= 0 AND \"Subtotal\" >= 0"));
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId);
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
            e.HasIndex(x => new { x.VentaId, x.ProductoId }).IsUnique();
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            e.Property(x => x.Descuento).HasPrecision(18, 2);
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
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId);
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.VentaId, x.ProductoId, x.AlmacenId, x.Tipo }).IsUnique();
        });
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        ValidarInmutabilidad();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
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
