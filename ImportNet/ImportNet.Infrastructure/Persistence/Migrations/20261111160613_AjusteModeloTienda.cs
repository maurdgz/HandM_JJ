using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjusteModeloTienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoImportacionUsd",
                table: "Producto",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambioImportacion",
                table: "Producto",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Importacion",
                table: "Producto",
                sql: "(\"CostoImportacionUsd\" IS NULL AND \"TipoCambioImportacion\" IS NULL) OR (\"CostoImportacionUsd\" IS NOT NULL AND \"TipoCambioImportacion\" IS NOT NULL AND \"CostoImportacionUsd\" >= 0 AND \"TipoCambioImportacion\" > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Importacion",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CostoImportacionUsd",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "TipoCambioImportacion",
                table: "Producto");
        }
    }
}
