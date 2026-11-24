using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtegerEntradasCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Movimiento_EntradaCompraProductoAlmacen",
                table: "MovimientoInventario",
                columns: new[] { "CompraId", "ProductoId", "AlmacenId" },
                unique: true,
                filter: "\"CompraId\" IS NOT NULL AND \"Tipo\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Movimiento_EntradaCompraProductoAlmacen",
                table: "MovimientoInventario");
        }
    }
}
