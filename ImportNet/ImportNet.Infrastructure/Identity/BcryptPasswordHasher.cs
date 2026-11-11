using Microsoft.AspNetCore.Identity;

namespace ImportNet.Infrastructure.Identity;

public sealed class BcryptPasswordHasher : ImportNet.Application.Security.IPasswordHasher
{
    // SHA-384 prehash prevents BCrypt's 72-byte truncation, including Unicode passwords.
    private const string Prefix = "{bcrypt-sha384}";
    public string Hash(string password) => Prefix + BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
    public bool Verificar(string password, string hash)
    {
        if (!hash.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        try { return BCrypt.Net.BCrypt.EnhancedVerify(password, hash[Prefix.Length..]); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}

// Kept only as a bridge to existing account storage, never as the HTTP authentication scheme.
internal sealed class IdentityPasswordTransition(ImportNet.Application.Security.IPasswordHasher bcrypt)
    : IPasswordHasher<ApplicationUser>
{
    private readonly PasswordHasher<ApplicationUser> legacy = new();
    public string HashPassword(ApplicationUser user, string password) => bcrypt.Hash(password);
    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.StartsWith("{bcrypt-sha384}", StringComparison.Ordinal))
            return bcrypt.Verificar(providedPassword, hashedPassword) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        try
        {
            return legacy.VerifyHashedPassword(user, hashedPassword, providedPassword) == PasswordVerificationResult.Failed
                ? PasswordVerificationResult.Failed : PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
    }
}
