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
        try
        {
            bytes = Convert.FromBase64String(config["Jwt:SigningKey"] ?? "");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Jwt:SigningKey debe ser Base64 de al menos 32 bytes aleatorios.");
        }

        if (bytes.Length < 32)
            throw new InvalidOperationException("Configure emisor, audiencia y clave JWT de al menos 32 bytes en secretos.");

        Key = new SymmetricSecurityKey(bytes);
    }
}

public sealed class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(JwtSettings settings)
    {
        _settings = settings;
    }

    public static string Version(string securityStamp)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
    }

    public TokenDto Emitir(Usuario usuario)
    {
        var ahora = DateTime.UtcNow;
        var expira = ahora.AddHours(2);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("empleado_id", usuario.Empleado.Id.ToString()),
            new Claim("access_version", Version(usuario.VersionAcceso))
        };

        foreach (var rol in usuario.Roles)
            claims.Add(new Claim("role", rol));

        var credenciales = new SigningCredentials(_settings.Key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims, ahora, expira, credenciales);

        return new TokenDto(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
