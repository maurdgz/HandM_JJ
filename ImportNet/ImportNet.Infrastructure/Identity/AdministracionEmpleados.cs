using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Identity;

internal sealed class AdministracionEmpleados : IAdministracionEmpleados
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public AdministracionEmpleados(AppDbContext context, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _context = context;
        _users = users;
        _roles = roles;
    }

    public async Task<IReadOnlyList<EmpleadoDto>> Listar(string? busqueda, bool? activo, CancellationToken ct)
    {
        var query = _context.Set<Empleado>().AsNoTracking();

        if (activo != null)
            query = query.Where(e => e.Activo == activo);

        var empleados = await query
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombre)
            .ToListAsync(ct);

        var resultado = new List<EmpleadoDto>();
        foreach (var empleado in empleados)
        {
            var dto = await CrearDto(empleado);
            if (Coincide(dto, busqueda))
                resultado.Add(dto);
        }

        return resultado;
    }

    public async Task<EmpleadoDto?> Obtener(int id, CancellationToken ct)
    {
        var empleado = await _context.Set<Empleado>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (empleado == null)
            return null;

        return await CrearDto(empleado);
    }

    public async Task<int> Registrar(EmpleadoDatos datos, string password, bool activo, string[] roles, CancellationToken ct)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            await ComprobarUnicos(datos, null, ct);
            await PrepararRoles();

            var user = new ApplicationUser { UserName = datos.Email, Email = datos.Email, PhoneNumber = datos.Telefono };
            Exigir(await _users.CreateAsync(user, password));
            Exigir(await _users.AddToRolesAsync(user, roles));

            var empleado = new Empleado { UsuarioId = user.Id, Activo = activo };
            Copiar(empleado, datos);
            _context.Set<Empleado>().Add(empleado);

            await _context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return empleado.Id;
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            throw new ConflictoException("Documento o correo duplicado, referencia inválida o cambio concurrente.");
        }
    }

    public async Task Actualizar(int id, EmpleadoDatos datos, bool activo, string[] roles, CancellationToken ct)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var empleado = await Cargar(id, ct);
            await ComprobarUnicos(datos, empleado.UsuarioId, ct);

            var user = await _users.FindByIdAsync(empleado.UsuarioId);
            if (user == null)
                throw new ConflictoException("El empleado no tiene una cuenta válida.");

            await GuardarAcceso(empleado, user, activo, roles);
            Copiar(empleado, datos);
            Exigir(await _users.SetEmailAsync(user, datos.Email));
            Exigir(await _users.SetUserNameAsync(user, datos.Email));
            Exigir(await _users.SetPhoneNumberAsync(user, datos.Telefono));

            await _context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            throw new ConflictoException("Documento o correo duplicado, referencia inválida o cambio concurrente.");
        }
    }

    public async Task CambiarAcceso(int id, bool? activo, string[]? roles, CancellationToken ct)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);

        var empleado = await Cargar(id, ct);
        var user = await _users.FindByIdAsync(empleado.UsuarioId);
        if (user == null)
            throw new ConflictoException("Cuenta no encontrada.");

        var nuevoEstado = activo ?? empleado.Activo;
        var nuevosRoles = roles ?? (await _users.GetRolesAsync(user)).ToArray();

        await GuardarAcceso(empleado, user, nuevoEstado, nuevosRoles);
        await _context.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    private async Task GuardarAcceso(Empleado empleado, ApplicationUser user, bool activo, string[] roles)
    {
        var actuales = await _users.GetRolesAsync(user);

        var eraAdministrador = empleado.Activo && actuales.Contains(RolesImportNet.Administrador);
        var dejaDeSerAdministrador = !activo || !roles.Contains(RolesImportNet.Administrador);

        if (eraAdministrador && dejaDeSerAdministrador && !await HayOtroAdministrador(empleado.Id))
            throw new ConflictoException("No se puede desactivar ni quitar el rol al último Administrador activo.");

        await PrepararRoles();
        Exigir(await _users.RemoveFromRolesAsync(user, actuales.Except(roles)));
        Exigir(await _users.AddToRolesAsync(user, roles.Except(actuales)));
        empleado.Activo = activo;
        Exigir(await _users.UpdateSecurityStampAsync(user));
    }

    private async Task<bool> HayOtroAdministrador(int empleadoId)
    {
        var administradores = await _users.GetUsersInRoleAsync(RolesImportNet.Administrador);
        var ids = administradores.Select(u => u.Id).ToList();

        return await _context.Set<Empleado>().AnyAsync(e => ids.Contains(e.UsuarioId) && e.Activo && e.Id != empleadoId);
    }

    private async Task<EmpleadoDto> CrearDto(Empleado empleado)
    {
        var user = await _users.FindByIdAsync(empleado.UsuarioId);
        var roles = user == null ? new List<string>() : (await _users.GetRolesAsync(user)).Order().ToList();

        return new EmpleadoDto(empleado.Id, empleado.UsuarioId, empleado.Nombre, empleado.Apellidos, empleado.DocumentoIdentidad,
            user?.Email ?? "", user?.PhoneNumber ?? "", empleado.Cargo, empleado.Area, empleado.Activo, roles);
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

    private async Task ComprobarUnicos(EmpleadoDatos datos, string? usuarioId, CancellationToken ct)
    {
        var documentoUsado = await _context.Set<Empleado>()
            .AnyAsync(e => e.DocumentoIdentidad == datos.DocumentoIdentidad && e.UsuarioId != usuarioId, ct);
        if (documentoUsado)
            throw new ConflictoException("El documento ya está registrado.");

        var user = await _users.FindByEmailAsync(datos.Email);
        if (user != null && user.Id != usuarioId)
            throw new ConflictoException("El correo ya está registrado.");
    }

    private async Task<Empleado> Cargar(int id, CancellationToken ct)
    {
        var empleado = await _context.Set<Empleado>().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (empleado == null)
            throw new NoEncontradoException("Empleado no encontrado.");

        return empleado;
    }

    private async Task PrepararRoles()
    {
        foreach (var rol in RolesImportNet.Todos)
        {
            if (!await _roles.RoleExistsAsync(rol))
                Exigir(await _roles.CreateAsync(new IdentityRole(rol)));
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

    private static bool EsDuplicado(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: "23505" or "23503" };
    }

    private static void Exigir(IdentityResult result)
    {
        if (result.Succeeded)
            return;

        if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName" || e.Code == "ConcurrencyFailure"))
            throw new ConflictoException("La cuenta está duplicada o cambió concurrentemente. Recargue los datos.");

        throw new ReglaNegocioException("Identity rechazó los datos. La contraseña debe incluir mayúscula, minúscula, número y símbolo.");
    }
}
