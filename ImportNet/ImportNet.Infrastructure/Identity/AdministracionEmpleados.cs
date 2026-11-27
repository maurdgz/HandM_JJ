using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Identity;

internal sealed class AdministracionEmpleados(
    AppDbContext context,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles) : IAdministracionEmpleados
{
    public async Task<IReadOnlyList<EmpleadoDto>> ListarAsync(string? busqueda, bool? activo, CancellationToken ct = default)
    {
        var query = context.Set<Empleado>().AsNoTracking();

        if (activo != null)
            query = query.Where(e => e.Activo == activo);

        var empleados = await query
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombre)
            .ToListAsync(ct);

        var resultado = new List<EmpleadoDto>();
        foreach (var empleado in empleados)
        {
            var dto = await CrearDtoAsync(empleado);
            if (Coincide(dto, busqueda))
                resultado.Add(dto);
        }

        return resultado;
    }

    public async Task<EmpleadoDto?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var empleado = await context.Set<Empleado>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return empleado == null ? null : await CrearDtoAsync(empleado);
    }

    public async Task<int> RegistrarAsync(EmpleadoDatos datos, string password, bool activo, string[] rolesSeleccionados, CancellationToken ct = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(ct);

        try
        {
            await ComprobarUnicosAsync(datos, null, ct);
            await PrepararRolesAsync();

            var user = new ApplicationUser { UserName = datos.Email, Email = datos.Email, PhoneNumber = datos.Telefono };
            Exigir(await users.CreateAsync(user, password));
            Exigir(await users.AddToRolesAsync(user, rolesSeleccionados));

            var empleado = new Empleado { UsuarioId = user.Id, Activo = activo };
            Copiar(empleado, datos);
            context.Set<Empleado>().Add(empleado);

            await context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return empleado.Id;
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            throw new ConflictoException("Documento o correo duplicado, referencia inválida o cambio concurrente.");
        }
    }

    public async Task ActualizarAsync(int id, EmpleadoDatos datos, bool activo, string[] rolesSeleccionados, CancellationToken ct = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var empleado = await CargarAsync(id, ct);
            await ComprobarUnicosAsync(datos, empleado.UsuarioId, ct);

            var user = await users.FindByIdAsync(empleado.UsuarioId)
                ?? throw new ConflictoException("El empleado no tiene una cuenta válida.");

            await GuardarAccesoAsync(empleado, user, activo, rolesSeleccionados);
            Copiar(empleado, datos);
            Exigir(await users.SetEmailAsync(user, datos.Email));
            Exigir(await users.SetUserNameAsync(user, datos.Email));
            Exigir(await users.SetPhoneNumberAsync(user, datos.Telefono));

            await context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            throw new ConflictoException("Documento o correo duplicado, referencia inválida o cambio concurrente.");
        }
    }

    public async Task CambiarAccesoAsync(int id, bool? activo, string[]? rolesSeleccionados, CancellationToken ct = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(ct);

        var empleado = await CargarAsync(id, ct);
        var user = await users.FindByIdAsync(empleado.UsuarioId)
            ?? throw new ConflictoException("Cuenta no encontrada.");

        var nuevoEstado = activo ?? empleado.Activo;
        var nuevosRoles = rolesSeleccionados ?? [.. await users.GetRolesAsync(user)];

        await GuardarAccesoAsync(empleado, user, nuevoEstado, nuevosRoles);
        await context.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    private async Task GuardarAccesoAsync(Empleado empleado, ApplicationUser user, bool activo, string[] rolesSeleccionados)
    {
        var actuales = await users.GetRolesAsync(user);

        var eraAdministrador = empleado.Activo && actuales.Contains(RolesImportNet.Administrador);
        var dejaDeSerAdministrador = !activo || !rolesSeleccionados.Contains(RolesImportNet.Administrador);

        if (eraAdministrador && dejaDeSerAdministrador && !await HayOtroAdministradorAsync(empleado.Id))
            throw new ConflictoException("No se puede desactivar ni quitar el rol al último Administrador activo.");

        await PrepararRolesAsync();
        Exigir(await users.RemoveFromRolesAsync(user, actuales.Except(rolesSeleccionados)));
        Exigir(await users.AddToRolesAsync(user, rolesSeleccionados.Except(actuales)));
        empleado.Activo = activo;
        Exigir(await users.UpdateSecurityStampAsync(user));
    }

    private async Task<bool> HayOtroAdministradorAsync(int empleadoId)
    {
        var administradores = await users.GetUsersInRoleAsync(RolesImportNet.Administrador);
        var ids = administradores.Select(u => u.Id).ToList();

        return await context.Set<Empleado>().AnyAsync(e => ids.Contains(e.UsuarioId) && e.Activo && e.Id != empleadoId);
    }

    private async Task<EmpleadoDto> CrearDtoAsync(Empleado empleado)
    {
        var user = await users.FindByIdAsync(empleado.UsuarioId);
        var rolesUsuario = user == null ? new List<string>() : (await users.GetRolesAsync(user)).Order().ToList();

        return new EmpleadoDto(
            empleado.Id,
            empleado.UsuarioId,
            empleado.Nombre,
            empleado.Apellidos,
            empleado.DocumentoIdentidad,
            user?.Email ?? "",
            user?.PhoneNumber ?? "",
            empleado.Cargo,
            empleado.Area,
            empleado.Activo,
            rolesUsuario);
    }

    private static bool Coincide(EmpleadoDto empleado, string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
            return true;

        var texto = busqueda.ToUpperInvariant();
        return empleado.Nombre.ToUpperInvariant().Contains(texto)
            || empleado.Apellidos.ToUpperInvariant().Contains(texto)
            || (empleado.DocumentoIdentidad != null && empleado.DocumentoIdentidad.Contains(texto))
            || empleado.Email.ToUpperInvariant().Contains(texto);
    }

    private async Task ComprobarUnicosAsync(EmpleadoDatos datos, string? usuarioId, CancellationToken ct)
    {
        var documentoUsado = await context.Set<Empleado>()
            .AnyAsync(e => e.DocumentoIdentidad == datos.DocumentoIdentidad && e.UsuarioId != usuarioId, ct);
        if (documentoUsado)
            throw new ConflictoException("El documento ya está registrado.");

        var user = await users.FindByEmailAsync(datos.Email);
        if (user != null && user.Id != usuarioId)
            throw new ConflictoException("El correo ya está registrado.");
    }

    private async Task<Empleado> CargarAsync(int id, CancellationToken ct)
    {
        return await context.Set<Empleado>().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NoEncontradoException("Empleado no encontrado.");
    }

    private async Task PrepararRolesAsync()
    {
        foreach (var rol in RolesImportNet.Todos)
        {
            if (!await roles.RoleExistsAsync(rol))
                Exigir(await roles.CreateAsync(new IdentityRole(rol)));
        }
    }

    private static void Copiar(Empleado empleado, EmpleadoDatos datos)
    {
        empleado.Nombre = datos.Nombre;
        empleado.Apellidos = datos.Apellidos;
        empleado.DocumentoIdentidad = datos.DocumentoIdentidad;
        empleado.Cargo = datos.Cargo;
        empleado.Area = datos.Area;
    }

    private static bool EsDuplicado(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" or "23503" };

    private static void Exigir(IdentityResult result)
    {
        if (result.Succeeded)
            return;

        if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName" || e.Code == "ConcurrencyFailure"))
            throw new ConflictoException("La cuenta está duplicada o cambió concurrentemente. Recargue los datos.");

        throw new ReglaNegocioException("Identity rechazó los datos. La contraseña debe incluir mayúscula, minúscula, número y símbolo.");
    }
}
