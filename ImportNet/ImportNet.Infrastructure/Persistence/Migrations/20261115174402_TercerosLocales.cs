using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace ImportNet.Infrastructure.Persistence.Migrations;
public partial class TercerosLocales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Do not assume Id=1 is available in an existing installation.
        migrationBuilder.Sql("""
            INSERT INTO "Cliente" ("Nombre", "NumeroDocumento", "TipoDocumento")
            SELECT 'Cliente varios', '00000000', 0
            WHERE NOT EXISTS (SELECT 1 FROM "Cliente" WHERE "NumeroDocumento" = '00000000');
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve potentially referenced customer data on rollback.
    }
}
