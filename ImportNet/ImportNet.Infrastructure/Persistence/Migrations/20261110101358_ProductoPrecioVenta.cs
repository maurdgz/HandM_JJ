using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    public partial class ProductoPrecioVenta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Peso",
                table: "Producto");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Producto");

            migrationBuilder.RenameColumn(
                name: "PrecioUsd",
                table: "Producto",
                newName: "PrecioVenta");

            migrationBuilder.AlterColumn<decimal>(
                name: "Peso",
                table: "Producto",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<string>(
                name: "PartidaArancelaria",
                table: "Producto",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoPromedio",
                table: "Producto",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Costo",
                table: "Producto",
                sql: "\"CostoPromedio\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Peso",
                table: "Producto",
                sql: "\"Peso\" IS NULL OR \"Peso\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Producto",
                sql: "\"PrecioVenta\" >= 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Costo",
                table: "Producto");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Peso",
                table: "Producto");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CostoPromedio",
                table: "Producto");

            migrationBuilder.RenameColumn(
                name: "PrecioVenta",
                table: "Producto",
                newName: "PrecioUsd");

            migrationBuilder.AlterColumn<decimal>(
                name: "Peso",
                table: "Producto",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PartidaArancelaria",
                table: "Producto",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Peso",
                table: "Producto",
                sql: "\"Peso\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Precio",
                table: "Producto",
                sql: "\"PrecioUsd\" >= 0");
        }
    }
}
