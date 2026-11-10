using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImportNet.Infrastructure.Persistence.Migrations
{
    public partial class VentaMostrador : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_OrdenVenta_OrdenVentaId",
                table: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "DetalleOrdenVenta");

            migrationBuilder.DropTable(
                name: "OrdenVenta");

            migrationBuilder.RenameColumn(
                name: "OrdenVentaId",
                table: "MovimientoInventario",
                newName: "VentaId");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientoInventario_OrdenVentaId_ProductoId_AlmacenId_Tipo",
                table: "MovimientoInventario",
                newName: "IX_MovimientoInventario_VentaId_ProductoId_AlmacenId_Tipo");

            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.Id);
                    table.CheckConstraint("CK_Venta_Anulacion", "(\"Estado\" = 0 AND \"MotivoAnulacion\" IS NULL) OR (\"Estado\" = 1 AND \"MotivoAnulacion\" IS NOT NULL)");
                    table.CheckConstraint("CK_Venta_Estado", "\"Estado\" IN (0, 1)");
                    table.CheckConstraint("CK_Venta_Importes", "\"Subtotal\" >= 0 AND \"Descuento\" >= 0 AND \"Descuento\" <= \"Subtotal\" AND \"Total\" = \"Subtotal\" - \"Descuento\"");
                    table.ForeignKey(
                        name: "FK_Venta_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Venta_Empleado_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVenta", x => x.Id);
                    table.CheckConstraint("CK_DetalleVenta_Importes", "\"Cantidad\" > 0 AND \"PrecioUnitario\" >= 0 AND \"Descuento\" >= 0 AND \"Subtotal\" >= 0");
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Venta_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_ProductoId",
                table: "DetalleVenta",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_VentaId_ProductoId",
                table: "DetalleVenta",
                columns: new[] { "VentaId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_ClienteId",
                table: "Venta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EmpleadoId",
                table: "Venta",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Fecha",
                table: "Venta",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Numero",
                table: "Venta",
                column: "Numero",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_Venta_VentaId",
                table: "MovimientoInventario",
                column: "VentaId",
                principalTable: "Venta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientoInventario_Venta_VentaId",
                table: "MovimientoInventario");

            migrationBuilder.DropTable(
                name: "DetalleVenta");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.RenameColumn(
                name: "VentaId",
                table: "MovimientoInventario",
                newName: "OrdenVentaId");

            migrationBuilder.RenameIndex(
                name: "IX_MovimientoInventario_VentaId_ProductoId_AlmacenId_Tipo",
                table: "MovimientoInventario",
                newName: "IX_MovimientoInventario_OrdenVentaId_ProductoId_AlmacenId_Tipo");

            migrationBuilder.CreateTable(
                name: "OrdenVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlmacenId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Incoterm = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaisDestino = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TipoCambio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenVenta", x => x.Id);
                    table.CheckConstraint("CK_OrdenVenta_Estado", "\"Estado\" BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_OrdenVenta_Importes", "\"TipoCambio\" > 0 AND \"Subtotal\" >= 0 AND \"Total\" = \"Subtotal\"");
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Almacen_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Empleado_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleOrdenVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    OrdenVentaId = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleOrdenVenta", x => x.Id);
                    table.CheckConstraint("CK_Detalle_Importes", "\"Cantidad\" > 0 AND \"PrecioUnitario\" >= 0 AND \"Subtotal\" >= 0");
                    table.ForeignKey(
                        name: "FK_DetalleOrdenVenta_OrdenVenta_OrdenVentaId",
                        column: x => x.OrdenVentaId,
                        principalTable: "OrdenVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetalleOrdenVenta_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetalleOrdenVenta_OrdenVentaId_ProductoId",
                table: "DetalleOrdenVenta",
                columns: new[] { "OrdenVentaId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetalleOrdenVenta_ProductoId",
                table: "DetalleOrdenVenta",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_AlmacenId",
                table: "OrdenVenta",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_ClienteId",
                table: "OrdenVenta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_EmpleadoId",
                table: "OrdenVenta",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Fecha",
                table: "OrdenVenta",
                column: "Fecha");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientoInventario_OrdenVenta_OrdenVentaId",
                table: "MovimientoInventario",
                column: "OrdenVentaId",
                principalTable: "OrdenVenta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
