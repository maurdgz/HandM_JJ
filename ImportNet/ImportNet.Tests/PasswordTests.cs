using ImportNet.Infrastructure.Identity;
using Xunit;

namespace ImportNet.Tests;

public sealed class PasswordTests
{
    [Fact]
    public void Bcrypt_NoTruncaContrasenasLargasYRechazaHashInvalido()
    {
        var hasher = new BcryptPasswordHasher();
        var password = new string('ñ', 80) + "A!123";
        var hash = hasher.Hash(password);
        Assert.True(hasher.Verificar(password, hash));
        Assert.False(hasher.Verificar(password + "x", hash));
        Assert.False(hasher.Verificar(password, "invalid"));
        Assert.DoesNotContain(password, hash);
    }
}
