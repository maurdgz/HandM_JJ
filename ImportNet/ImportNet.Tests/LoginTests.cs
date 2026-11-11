using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Auth;
using ImportNet.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ImportNet.Application;
using ImportNet.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ImportNet.Tests;

public sealed class LoginTests
{
    [Fact]
    public async Task Login_EmiteClaimsSeguros_YRechazaCredencialesIncorrectas()
    {
        var services = new ServiceCollection().AddLogging().AddApplication();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Jwt:Issuer"] = "test", ["Jwt:Audience"] = "test",
            ["Jwt:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64))
        }).Build();
        services.AddSingleton(new JwtSettings(config));
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IUsuarioRepository, Usuarios>();
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new LoginQuery("test@example.test", "correct"));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("u1", token.Subject);
        Assert.Equal("7", token.Claims.Single(c => c.Type == "empleado_id").Value);
        Assert.Equal("Administrador", token.Claims.Single(c => c.Type == "role").Value);
        Assert.InRange((token.ValidTo - token.ValidFrom).TotalHours, 1.999, 2.001);
        Assert.DoesNotContain("stamp-secret", result.AccessToken);
        await Assert.ThrowsAsync<AccesoNoPermitidoException>(() => mediator.Send(new LoginQuery("test@example.test", "wrong")));
    }
    private sealed class Usuarios : IUsuarioRepository
    {
        public Task<Usuario?> Autenticar(string email, string password, CancellationToken ct)
            => Task.FromResult(password == "correct" ? new Usuario("u1", email, "stamp-secret",
                new Empleado { Id = 7, UsuarioId = "u1" }, [new UsuarioRol("u1", new Rol("r1", "Administrador"))]) : null);
        public Task<Usuario?> Obtener(string id, CancellationToken ct) => throw new NotSupportedException();
    }
}
