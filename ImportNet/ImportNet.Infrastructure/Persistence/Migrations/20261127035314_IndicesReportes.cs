using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndicesReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_EmpleadoId",
                table: "Venta");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EmpleadoFecha",
                table: "Venta",
                columns: new[] { "EmpleadoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EstadoFecha",
                table: "Venta",
                columns: new[] { "Estado", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Compra_EstadoFecha",
                table: "Compra",
                columns: new[] { "Estado", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venta_EmpleadoFecha",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_EstadoFecha",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Compra_EstadoFecha",
                table: "Compra");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EmpleadoId",
                table: "Venta",
                column: "EmpleadoId");
        }
    }
}
