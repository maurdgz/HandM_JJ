using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<string>().HaveMaxLength(200);
        builder.Properties<decimal>().HavePrecision(18, 3);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        b.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();

        b.Entity<Empleado>(e =>
        {
            e.ToTable("Empleado");
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.Apellidos).HasMaxLength(100);
            e.Property(x => x.DocumentoIdentidad).HasMaxLength(20);
            e.HasIndex(x => x.DocumentoIdentidad).IsUnique();
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Empleado>(x => x.UsuarioId);
        });

        b.Entity<Almacen>(e =>
        {
            e.ToTable("Almacen");
            e.HasData(new Almacen { Id = Almacen.PrincipalId, Nombre = "Principal" });
        });

        b.Entity<Inventario>(e =>
        {
            e.ToTable("Inventario", t => t.HasCheckConstraint("CK_Inventario_Cantidad", "\"Cantidad\" >= 0"));
            e.HasIndex(x => new { x.ProductoId, x.AlmacenId }).IsUnique();
            e.HasOne(x => x.Producto).WithMany(p => p.Inventarios).HasForeignKey(x => x.ProductoId);
            e.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId);
            e.Property(x => x.Version).IsRowVersion();
        });

        b.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedor", t => t.HasCheckConstraint("CK_Proveedor_Tipo", "\"Tipo\" IN (0, 1)"));
            e.Property(x => x.Pais).HasMaxLength(100);
        });

        b.Entity<ProveedorProducto>(e =>
        {
            e.ToTable("ProveedorProducto");
            e.HasKey(x => new { x.ProveedorId, x.ProductoId });
            e.HasOne<Proveedor>().WithMany(x => x.Suministros).HasForeignKey(x => x.ProveedorId);
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
        });

        b.Entity<Cliente>(e =>
        {
            e.ToTable("Cliente", t => t.HasCheckConstraint("CK_Cliente_TipoDocumento", "\"TipoDocumento\" IN (0, 1)"));
            e.HasData(new Cliente { Id = 1, TipoDocumento = TipoDocumento.Dni, NumeroDocumento = "00000000", Nombre = "Cliente varios" });
            e.Property(x => x.NumeroDocumento).HasMaxLength(11);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasIndex(x => x.NumeroDocumento).IsUnique();
        });

        b.Entity<Venta>(e =>
        {
            e.ToTable("Venta", t =>
            {
                t.HasCheckConstraint("CK_Venta_Importes", "\"Subtotal\" >= 0 AND \"Descuento\" >= 0 AND \"Descuento\" <= \"Subtotal\" AND \"Total\" = \"Subtotal\" - \"Descuento\"");
                t.HasCheckConstraint("CK_Venta_Estado", "\"Estado\" IN (0, 1)");
                t.HasCheckConstraint("CK_Venta_Anulacion", "(\"Estado\" = 0 AND \"MotivoAnulacion\" IS NULL) OR (\"Estado\" = 1 AND \"MotivoAnulacion\" IS NOT NULL)");
            });
            e.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Descuento).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.MotivoAnulacion).HasMaxLength(300);
            e.HasIndex(x => x.Numero).IsUnique();
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.Estado, x.Fecha }).HasDatabaseName("IX_Venta_EstadoFecha");
            e.HasIndex(x => new { x.EmpleadoId, x.Fecha }).HasDatabaseName("IX_Venta_EmpleadoFecha");
            e.Property(x => x.Version).IsRowVersion();
        });

        b.Entity<DetalleVenta>(e =>
        {
            e.ToTable("DetalleVenta", t => t.HasCheckConstraint("CK_DetalleVenta_Importes", "\"Cantidad\" > 0 AND \"PrecioUnitario\" >= 0 AND \"Descuento\" >= 0 AND \"Subtotal\" >= 0"));
            e.HasOne(x => x.Venta).WithMany(v => v.Detalles).HasForeignKey(x => x.VentaId);
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId);
            e.HasIndex(x => new { x.VentaId, x.ProductoId }).IsUnique();
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            e.Property(x => x.Descuento).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
        });

        b.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("MovimientoInventario", t =>
            {
                t.HasCheckConstraint("CK_Movimiento_Cantidad", "(\"Tipo\" IN (0, 1) AND \"Cantidad\" > 0) OR (\"Tipo\" = 2 AND \"Cantidad\" <> 0)");
                t.HasCheckConstraint("CK_Movimiento_MotivoAjuste", "(\"Tipo\" = 2 AND \"MotivoAjuste\" IN (0, 1, 2, 3)) OR (\"Tipo\" <> 2 AND \"MotivoAjuste\" IS NULL)");
            });
            e.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId);
            e.HasOne<Almacen>().WithMany().HasForeignKey(x => x.AlmacenId);
            e.HasOne(x => x.Empleado).WithMany().HasForeignKey(x => x.EmpleadoId);
            e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId);
            e.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId);
            e.HasOne<Compra>().WithMany().HasForeignKey(x => x.CompraId);
            e.Property(x => x.Observacion).HasMaxLength(AjusteInventario.ObservacionMaxima);
            e.HasIndex(x => x.Fecha);
            e.HasIndex(x => new { x.VentaId, x.ProductoId, x.AlmacenId, x.Tipo }).IsUnique();
            e.HasIndex(x => x.CompraId).HasDatabaseName("IX_MovimientoInventario_CompraId");
            e.HasIndex(x => new { x.CompraId, x.ProductoId, x.AlmacenId })
                .IsUnique()
                .HasDatabaseName("UX_Movimiento_EntradaCompraProductoAlmacen")
                .HasFilter("\"CompraId\" IS NOT NULL AND \"Tipo\" = 0");
            e.HasIndex(x => new { x.ProductoId, x.Fecha }).HasDatabaseName("IX_Movimiento_ProductoFecha");
        });

        foreach (var fk in b.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        b.Entity<Compra>().HasMany(x => x.Lineas).WithOne().HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Cascade);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        foreach (var entrada in ChangeTracker.Entries<Compra>())
        {
            var cambiada = entrada.State == EntityState.Modified || entrada.State == EntityState.Deleted;
            if (cambiada && entrada.OriginalValues.GetValue<EstadoCompra>(nameof(Compra.Estado)) == EstadoCompra.Recibida)
                throw new ConflictoException("Una compra recibida es inmutable.");
        }

        foreach (var entrada in ChangeTracker.Entries<MovimientoInventario>())
        {
            if (entrada.State == EntityState.Modified || entrada.State == EntityState.Deleted)
                throw new ReglaNegocioException("Los movimientos son inmutables; registre un ajuste.");
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
}
