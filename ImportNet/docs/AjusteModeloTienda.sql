START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160613_AjusteModeloTienda') THEN
    ALTER TABLE "Producto" ADD "CostoImportacionUsd" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160613_AjusteModeloTienda') THEN
    ALTER TABLE "Producto" ADD "TipoCambioImportacion" numeric(18,6);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160613_AjusteModeloTienda') THEN
    ALTER TABLE "Producto" ADD CONSTRAINT "CK_Producto_Importacion" CHECK (("CostoImportacionUsd" IS NULL AND "TipoCambioImportacion" IS NULL) OR ("CostoImportacionUsd" IS NOT NULL AND "TipoCambioImportacion" IS NOT NULL AND "CostoImportacionUsd" >= 0 AND "TipoCambioImportacion" > 0));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160613_AjusteModeloTienda') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111160613_AjusteModeloTienda', '10.0.11');
    END IF;
END $EF$;
COMMIT;

