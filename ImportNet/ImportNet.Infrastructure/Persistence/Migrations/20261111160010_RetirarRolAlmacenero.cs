using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetirarRolAlmacenero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
                             WHERE r."NormalizedName" = 'ALMACENERO')
                     OR EXISTS (SELECT 1 FROM "AspNetRoleClaims" rc JOIN "AspNetRoles" r ON r."Id" = rc."RoleId"
                                WHERE r."NormalizedName" = 'ALMACENERO') THEN
                    RAISE EXCEPTION 'Almacenero conserva asignaciones. Revise sus accesos explicitamente antes de retirarlo.';
                  END IF;
                END $$;
                DELETE FROM "AspNetRoles" WHERE "NormalizedName" = 'ALMACENERO';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally does not restore a role forbidden by the current access matrix.
        }
    }
}
