using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    public partial class AlmacenPrincipal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Almacen",
                columns: new[] { "Id", "Nombre" },
                values: new object[] { 1, "Principal" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Almacen",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
