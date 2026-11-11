using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ImportNet.Infrastructure.Identity;

public sealed class JwtSettings
{
    public string Issuer { get; }
    public string Audience { get; }
    public SymmetricSecurityKey Key { get; }
    public JwtSettings(IConfiguration config)
    {
        Issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Configure Jwt:Issuer.");
        Audience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Configure Jwt:Audience.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(config["Jwt:SigningKey"] ?? ""); }
        catch (FormatException) { throw new InvalidOperationException("Jwt:SigningKey debe ser Base64 de al menos 32 bytes aleatorios."); }
        if (bytes.Length < 32 || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configure emisor, audiencia y clave JWT de al menos 32 bytes en secretos.");
        Key = new SymmetricSecurityKey(bytes);
    }
}

public sealed class JwtService(JwtSettings settings) : IJwtService
{
    public static string Version(string stamp) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stamp)));
    public TokenDto Emitir(Usuario usuario)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddHours(2);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("empleado_id", usuario.Empleado.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("access_version", Version(usuario.VersionAcceso))
        };
        claims.AddRange(usuario.Roles.Select(r => new Claim("role", r.Rol.Nombre)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now, expires,
            new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
