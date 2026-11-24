using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VincularMovimientosCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompraId",
                table: "MovimientoInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_CompraId",
                table: "MovimientoInventario",
                column: "CompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_Compra_CompraId",
                table: "MovimientoInventario",
                column: "CompraId",
                principalTable: "Compra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_Compra_CompraId",
                table: "MovimientoInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_CompraId",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "CompraId",
                table: "MovimientoInventario");
        }
    }
}
