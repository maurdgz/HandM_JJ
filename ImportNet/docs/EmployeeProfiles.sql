START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    DROP INDEX "EmailIndex";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    ALTER TABLE "Empleado" ADD "Apellidos" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    ALTER TABLE "Empleado" ADD "DocumentoIdentidad" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    CREATE UNIQUE INDEX "IX_Empleado_DocumentoIdentidad" ON "Empleado" ("DocumentoIdentidad");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    CREATE UNIQUE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261107151357_EmployeeProfiles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261107151357_EmployeeProfiles', '10.0.11');
    END IF;
END $EF$;
COMMIT;
