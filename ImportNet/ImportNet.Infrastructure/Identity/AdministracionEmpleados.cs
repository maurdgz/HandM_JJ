using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Identity;

internal sealed class AdministracionEmpleados(AppDbContext db, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles, IUsuarioActual actor) : IAdministracionEmpleados
{
    public Task<IReadOnlyList<EmpleadoDto>> Listar(string? busqueda, bool? activo, CancellationToken ct)
        => Consultar(null, busqueda, activo, ct);
    public async Task<EmpleadoDto?> Obtener(int id, CancellationToken ct)
        => (await Consultar(id, null, null, ct)).SingleOrDefault();

    private async Task<IReadOnlyList<EmpleadoDto>> Consultar(int? id, string? busqueda, bool? activo, CancellationToken ct)
    {
        var query = from e in db.Set<Empleado>().AsNoTracking()
                    join u in db.Users.AsNoTracking() on e.UsuarioId equals u.Id
                    select new { Empleado = e, Email = u.Email ?? "", Telefono = u.PhoneNumber ?? "" };
        if (id.HasValue) query = query.Where(x => x.Empleado.Id == id);
        if (activo.HasValue) query = query.Where(x => x.Empleado.Activo == activo);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.ToUpperInvariant();
            query = query.Where(x => x.Empleado.Nombre.ToUpper().Contains(texto) || x.Empleado.Apellidos.ToUpper().Contains(texto)
                || (x.Empleado.DocumentoIdentidad != null && x.Empleado.DocumentoIdentidad.Contains(texto)) || x.Email.ToUpper().Contains(texto));
        }
        var rows = await query.OrderBy(x => x.Empleado.Apellidos).ThenBy(x => x.Empleado.Nombre).ToListAsync(ct);
        var ids = rows.Select(x => x.Empleado.UsuarioId).ToArray();
        var assigned = await (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id
            where ids.Contains(ur.UserId) select new { ur.UserId, Nombre = role.Name! }).ToListAsync(ct);
        var lookup = assigned.ToLookup(x => x.UserId, x => x.Nombre);
        return rows.Select(x => new EmpleadoDto(x.Empleado.Id, x.Empleado.UsuarioId, x.Empleado.Nombre, x.Empleado.Apellidos,
            x.Empleado.DocumentoIdentidad, x.Email, x.Telefono, x.Empleado.Cargo, x.Empleado.Area, x.Empleado.Activo,
            lookup[x.Empleado.UsuarioId].Order().ToArray())).ToArray();
    }

    public Task<int> Registrar(EmpleadoDatos datos, string password, bool activo, string[] assigned, CancellationToken ct)
        => Transaccion(async () => {
            await ComprobarUnicos(datos, null, ct);
            await PrepararRoles();
            var user = new ApplicationUser { UserName = datos.Email, Email = datos.Email, PhoneNumber = datos.Telefono };
            Exigir(await users.CreateAsync(user, password));
            Exigir(await users.AddToRolesAsync(user, assigned));
            var empleado = new Empleado { UsuarioId = user.Id, Activo = activo };
            Copiar(empleado, datos);
            db.Add(empleado);
            await db.SaveChangesAsync(ct);
            return empleado.Id;
        }, ct);

    public async Task Actualizar(int id, EmpleadoDatos datos, bool activo, string[] assigned, CancellationToken ct)
        => await Transaccion(async () => {
            var empleado = await Cargar(id, ct);
            await ComprobarUnicos(datos, empleado.UsuarioId, ct);
            var user = await users.FindByIdAsync(empleado.UsuarioId) ?? throw new ConflictoException("El empleado no tiene una cuenta válida.");
            await GuardarAcceso(empleado, user, activo, assigned, ct);
            Copiar(empleado, datos);
            Exigir(await users.SetEmailAsync(user, datos.Email));
            Exigir(await users.SetUserNameAsync(user, datos.Email));
            Exigir(await users.SetPhoneNumberAsync(user, datos.Telefono));
            return id;
        }, ct);

    public async Task CambiarAcceso(int id, bool? activo, string[]? assigned, CancellationToken ct)
        => await Transaccion(async () => {
            var empleado = await Cargar(id, ct);
            var user = await users.FindByIdAsync(empleado.UsuarioId) ?? throw new ConflictoException("Cuenta no encontrada.");
            await GuardarAcceso(empleado, user, activo ?? empleado.Activo, assigned ?? (await users.GetRolesAsync(user)).ToArray(), ct);
            return id;
        }, ct);

    private async Task GuardarAcceso(Empleado empleado, ApplicationUser user, bool activo, string[] assigned, CancellationToken ct)
    {
        var actuales = await users.GetRolesAsync(user);
        if (empleado.Activo && actuales.Contains(RolesImportNet.Administrador)
            && (!activo || !assigned.Contains(RolesImportNet.Administrador)))
        {
            var otro = await (from e in db.Set<Empleado>() join ur in db.UserRoles on e.UsuarioId equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where e.Id != empleado.Id && e.Activo && r.Name == RolesImportNet.Administrador select e.Id).AnyAsync(ct);
            if (!otro) throw new ConflictoException("No se puede desactivar ni quitar el rol al último Administrador activo.");
        }
        await PrepararRoles();
        Exigir(await users.RemoveFromRolesAsync(user, actuales.Except(assigned)));
        Exigir(await users.AddToRolesAsync(user, assigned.Except(actuales)));
        empleado.Activo = activo;
        Exigir(await users.UpdateSecurityStampAsync(user));
    }
    private async Task ComprobarUnicos(EmpleadoDatos datos, string? usuarioId, CancellationToken ct)
    {
        if (await db.Set<Empleado>().AnyAsync(e => e.DocumentoIdentidad == datos.DocumentoIdentidad && e.UsuarioId != usuarioId, ct))
            throw new ConflictoException("El documento ya está registrado.");
        var user = await users.FindByEmailAsync(datos.Email);
        if (user != null && user.Id != usuarioId) throw new ConflictoException("El correo ya está registrado.");
    }
    private async Task<Empleado> Cargar(int id, CancellationToken ct)
        => await db.Set<Empleado>().SingleOrDefaultAsync(e => e.Id == id, ct) ?? throw new NoEncontradoException("Empleado no encontrado.");
    private async Task PrepararRoles()
    {
        foreach (var role in RolesImportNet.Todos)
            if (!await roles.RoleExistsAsync(role)) Exigir(await roles.CreateAsync(new IdentityRole(role)));
    }
    private static void Copiar(Empleado empleado, EmpleadoDatos datos)
    {
        empleado.Nombre = datos.Nombre; empleado.Apellidos = datos.Apellidos; empleado.DocumentoIdentidad = datos.DocumentoIdentidad;
        empleado.Cargo = datos.Cargo; empleado.Area = datos.Area;
    }
    private static void Exigir(IdentityResult result)
    {
        if (result.Succeeded) return;
        if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName" or "ConcurrencyFailure"))
            throw new ConflictoException("La cuenta está duplicada o cambió concurrentemente. Recargue los datos.");
        throw new ReglaNegocioException("Identity rechazó los datos. La contraseña debe incluir mayúscula, minúscula, número y símbolo.");
    }

    private async Task<int> Transaccion(Func<Task<int>> operacion, CancellationToken ct)
    {
        var externa = db.Database.CurrentTransaction;
        await using var propia = externa is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var savepoint = "empleados_" + Guid.NewGuid().ToString("N");
        if (externa != null) await externa.CreateSavepointAsync(savepoint, ct);
        try
        {
            // All account changes share this transaction-scoped lock, including concurrent administrators.
            if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(480071126)", ct);
            await actor.ExigirAdministrador(ct);
            var id = await operacion();
            await db.SaveChangesAsync(ct);
            if (propia != null) await propia.CommitAsync(ct);
            else await externa!.ReleaseSavepointAsync(savepoint, ct);
            return id;
        }
        catch (Exception ex)
        {
            if (externa != null) await externa.RollbackToSavepointAsync(savepoint, ct);
            if (ex is DbUpdateConcurrencyException || ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23503" } })
                throw new ConflictoException("Documento o correo duplicado, referencia inválida o cambio concurrente.");
            throw;
        }
    }
}
