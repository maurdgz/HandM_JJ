START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM "Producto") THEN
        RAISE EXCEPTION 'PrecioUsd requiere conversion explicita a PEN. Defina precios antes de migrar; no se modificaron productos.';
      END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" DROP CONSTRAINT "CK_Producto_Peso";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" DROP CONSTRAINT "CK_Producto_Precio";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" RENAME COLUMN "PrecioUsd" TO "PrecioVenta";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ALTER COLUMN "Peso" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ALTER COLUMN "PartidaArancelaria" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ADD "Activo" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ADD "CostoPromedio" numeric(18,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ADD CONSTRAINT "CK_Producto_Costo" CHECK ("CostoPromedio" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ADD CONSTRAINT "CK_Producto_Peso" CHECK ("Peso" IS NULL OR "Peso" > 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    ALTER TABLE "Producto" ADD CONSTRAINT "CK_Producto_Precio" CHECK ("PrecioVenta" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101358_ProductoPrecioVenta') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110101358_ProductoPrecioVenta', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM "Cliente") THEN
        RAISE EXCEPTION 'ClienteLocal requiere un mapeo de documentos y archivo de datos anteriores. No se eliminaron clientes.';
      END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" DROP COLUMN "Contacto";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" DROP COLUMN "Empresa";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" DROP COLUMN "IncotermPreferido";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" DROP COLUMN "MonedaPreferida";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" RENAME COLUMN "Pais" TO "Nombre";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" ADD "NumeroDocumento" character varying(11) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" ADD "Telefono" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" ADD "TipoDocumento" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    CREATE UNIQUE INDEX "IX_Cliente_NumeroDocumento" ON "Cliente" ("NumeroDocumento");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    ALTER TABLE "Cliente" ADD CONSTRAINT "CK_Cliente_TipoDocumento" CHECK ("TipoDocumento" IN (0, 1));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101430_ClienteLocal') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110101430_ClienteLocal', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM "OrdenVenta") OR EXISTS (SELECT 1 FROM "DetalleOrdenVenta")
         OR EXISTS (SELECT 1 FROM "MovimientoInventario" WHERE "OrdenVentaId" IS NOT NULL) THEN
        RAISE EXCEPTION 'VentaMostrador requiere preservar y mapear el historial existente. No se eliminaron ventas.';
      END IF;
    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    ALTER TABLE "MovimientoInventario" DROP CONSTRAINT "FK_MovimientoInventario_OrdenVenta_OrdenVentaId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    DROP TABLE "DetalleOrdenVenta";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    DROP TABLE "OrdenVenta";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    ALTER TABLE "MovimientoInventario" RENAME COLUMN "OrdenVentaId" TO "VentaId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    ALTER INDEX "IX_MovimientoInventario_OrdenVentaId_ProductoId_AlmacenId_Tipo" RENAME TO "IX_MovimientoInventario_VentaId_ProductoId_AlmacenId_Tipo";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE TABLE "Venta" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "Numero" integer NOT NULL,
        "EmpleadoId" integer NOT NULL,
        "ClienteId" integer,
        "Fecha" timestamp with time zone NOT NULL,
        "Estado" integer NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "Descuento" numeric(18,2) NOT NULL,
        "Total" numeric(18,2) NOT NULL,
        "MotivoAnulacion" character varying(300),
        "FechaAnulacion" timestamp with time zone,
        CONSTRAINT "PK_Venta" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Venta_Anulacion" CHECK (("Estado" = 0 AND "MotivoAnulacion" IS NULL) OR ("Estado" = 1 AND "MotivoAnulacion" IS NOT NULL)),
        CONSTRAINT "CK_Venta_Estado" CHECK ("Estado" IN (0, 1)),
        CONSTRAINT "CK_Venta_Importes" CHECK ("Subtotal" >= 0 AND "Descuento" >= 0 AND "Descuento" <= "Subtotal" AND "Total" = "Subtotal" - "Descuento"),
        CONSTRAINT "FK_Venta_Cliente_ClienteId" FOREIGN KEY ("ClienteId") REFERENCES "Cliente" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Venta_Empleado_EmpleadoId" FOREIGN KEY ("EmpleadoId") REFERENCES "Empleado" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE TABLE "DetalleVenta" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "VentaId" integer NOT NULL,
        "ProductoId" integer NOT NULL,
        "Cantidad" numeric(18,3) NOT NULL,
        "PrecioUnitario" numeric(18,2) NOT NULL,
        "Descuento" numeric(18,2) NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_DetalleVenta" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_DetalleVenta_Importes" CHECK ("Cantidad" > 0 AND "PrecioUnitario" >= 0 AND "Descuento" >= 0 AND "Subtotal" >= 0),
        CONSTRAINT "FK_DetalleVenta_Producto_ProductoId" FOREIGN KEY ("ProductoId") REFERENCES "Producto" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DetalleVenta_Venta_VentaId" FOREIGN KEY ("VentaId") REFERENCES "Venta" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE INDEX "IX_DetalleVenta_ProductoId" ON "DetalleVenta" ("ProductoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE UNIQUE INDEX "IX_DetalleVenta_VentaId_ProductoId" ON "DetalleVenta" ("VentaId", "ProductoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE INDEX "IX_Venta_ClienteId" ON "Venta" ("ClienteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE INDEX "IX_Venta_EmpleadoId" ON "Venta" ("EmpleadoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE INDEX "IX_Venta_Fecha" ON "Venta" ("Fecha");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    CREATE UNIQUE INDEX "IX_Venta_Numero" ON "Venta" ("Numero");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    ALTER TABLE "MovimientoInventario" ADD CONSTRAINT "FK_MovimientoInventario_Venta_VentaId" FOREIGN KEY ("VentaId") REFERENCES "Venta" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101515_VentaMostrador') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110101515_VentaMostrador', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101531_AlmacenPrincipal') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM "Almacen" WHERE "Id" = 1 AND "Nombre" <> 'Principal') THEN
        RAISE EXCEPTION 'El almacen 1 ya existe con otro nombre; revise su identidad antes de migrar.';
      END IF;
    END $$;
    INSERT INTO "Almacen" ("Id", "Nombre") VALUES (1, 'Principal') ON CONFLICT ("Id") DO NOTHING;
    SELECT setval(pg_get_serial_sequence('"Almacen"', 'Id'), GREATEST((SELECT max("Id") FROM "Almacen"), 1));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110101531_AlmacenPrincipal') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110101531_AlmacenPrincipal', '10.0.11');
    END IF;
END $EF$;
COMMIT;

