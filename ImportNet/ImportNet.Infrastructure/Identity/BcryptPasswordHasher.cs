using Microsoft.AspNetCore.Identity;

namespace ImportNet.Infrastructure.Identity;

public sealed class BcryptPasswordHasher : ImportNet.Application.Security.IPasswordHasher
{
    private const string Prefijo = "{bcrypt-sha384}";

    public string Hash(string password)
    {
        return Prefijo + BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
    }

    public bool Verificar(string password, string hash)
    {
        if (!hash.StartsWith(Prefijo))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, hash.Substring(Prefijo.Length));
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

internal sealed class IdentityPasswordHasher : IPasswordHasher<ApplicationUser>
{
    private readonly ImportNet.Application.Security.IPasswordHasher _bcrypt;
    private readonly PasswordHasher<ApplicationUser> _anterior = new PasswordHasher<ApplicationUser>();

    public IdentityPasswordHasher(ImportNet.Application.Security.IPasswordHasher bcrypt)
    {
        _bcrypt = bcrypt;
    }

    public string HashPassword(ApplicationUser user, string password)
    {
        return _bcrypt.Hash(password);
    }

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.StartsWith("{bcrypt-sha384}"))
        {
            return _bcrypt.Verificar(providedPassword, hashedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }

        try
        {
            var resultado = _anterior.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return resultado == PasswordVerificationResult.Failed
                ? PasswordVerificationResult.Failed
                : PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
