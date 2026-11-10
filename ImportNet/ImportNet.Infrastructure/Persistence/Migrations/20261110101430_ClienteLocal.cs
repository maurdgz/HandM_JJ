using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    public partial class ClienteLocal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contacto",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "IncotermPreferido",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "MonedaPreferida",
                table: "Cliente");

            migrationBuilder.RenameColumn(
                name: "Pais",
                table: "Cliente",
                newName: "Nombre");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Cliente",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Cliente",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumento",
                table: "Cliente",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_NumeroDocumento",
                table: "Cliente",
                column: "NumeroDocumento",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cliente_TipoDocumento",
                table: "Cliente",
                sql: "\"TipoDocumento\" IN (0, 1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cliente_NumeroDocumento",
                table: "Cliente");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cliente_TipoDocumento",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Cliente");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "Cliente",
                newName: "Pais");

            migrationBuilder.AddColumn<string>(
                name: "Contacto",
                table: "Cliente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "Cliente",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncotermPreferido",
                table: "Cliente",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MonedaPreferida",
                table: "Cliente",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
