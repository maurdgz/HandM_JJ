using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    public partial class AlmacenPrincipal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "Almacen" WHERE "Id" = 1 AND "Nombre" <> 'Principal') THEN
                    RAISE EXCEPTION 'El almacen 1 ya existe con otro nombre; revise su identidad antes de migrar.';
                  END IF;
                END $$;
                INSERT INTO "Almacen" ("Id", "Nombre") VALUES (1, 'Principal') ON CONFLICT ("Id") DO NOTHING;
                SELECT setval(pg_get_serial_sequence('"Almacen"', 'Id'), GREATEST((SELECT max("Id") FROM "Almacen"), 1));
                """);
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
