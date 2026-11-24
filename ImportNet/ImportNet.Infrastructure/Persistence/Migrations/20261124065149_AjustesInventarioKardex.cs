using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjustesInventarioKardex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientoInventario_ProductoId",
                table: "MovimientoInventario");

            migrationBuilder.AddColumn<int>(
                name: "MotivoAjuste",
                table: "MovimientoInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "MovimientoInventario",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_ProductoFecha",
                table: "MovimientoInventario",
                columns: new[] { "ProductoId", "Fecha" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimiento_MotivoAjuste",
                table: "MovimientoInventario",
                sql: "(\"Tipo\" = 2 AND \"MotivoAjuste\" IN (0, 1, 2, 3)) OR (\"Tipo\" <> 2 AND \"MotivoAjuste\" IS NULL)");

            // ExecuteUpdate, ExecuteDelete y SQL directo no pasan por SaveChanges; la base también lo impide.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    CREATE FUNCTION "BloquearCambioMovimiento"() RETURNS trigger
                    LANGUAGE plpgsql AS $$
                    BEGIN
                        RAISE EXCEPTION 'Los movimientos de inventario son inmutables; registre un ajuste.'
                            USING ERRCODE = '55000';
                    END
                    $$;

                    CREATE TRIGGER "TR_Movimiento_Inmutable"
                    BEFORE UPDATE OR DELETE ON "MovimientoInventario"
                    FOR EACH ROW EXECUTE FUNCTION "BloquearCambioMovimiento"();
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    DROP TRIGGER IF EXISTS "TR_Movimiento_Inmutable" ON "MovimientoInventario";
                    DROP FUNCTION IF EXISTS "BloquearCambioMovimiento"();
                    """);
            }

            migrationBuilder.DropIndex(
                name: "IX_Movimiento_ProductoFecha",
                table: "MovimientoInventario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimiento_MotivoAjuste",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "MotivoAjuste",
                table: "MovimientoInventario");

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "MovimientoInventario");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoInventario_ProductoId",
                table: "MovimientoInventario",
                column: "ProductoId");
        }
    }
}
