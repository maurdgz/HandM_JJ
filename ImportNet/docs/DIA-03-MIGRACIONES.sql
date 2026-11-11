START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160010_RetirarRolAlmacenero') THEN
    DO $$ BEGIN
      IF EXISTS (SELECT 1 FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
                 WHERE r."NormalizedName" = 'ALMACENERO')
         OR EXISTS (SELECT 1 FROM "AspNetRoleClaims" rc JOIN "AspNetRoles" r ON r."Id" = rc."RoleId"
                    WHERE r."NormalizedName" = 'ALMACENERO') THEN
        RAISE EXCEPTION 'Almacenero conserva asignaciones. Revise sus accesos explicitamente antes de retirarlo.';
      END IF;
    END $$;
    DELETE FROM "AspNetRoles" WHERE "NormalizedName" = 'ALMACENERO';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111160010_RetirarRolAlmacenero') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261111160010_RetirarRolAlmacenero', '10.0.11');
    END IF;
END $EF$;
COMMIT;

