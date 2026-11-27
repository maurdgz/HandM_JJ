using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Domain.Exceptions;
using Xunit;

namespace ImportNet.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task CreateCategoria_NombreVacio_LanzaValidacionExceptionConErroresDeCampo()
    {
        using var fixture = new ApplicationFixture();

        var ex = await Assert.ThrowsAsync<ValidacionException>(
            () => fixture.Mediator.Send(new CreateCategoriaCommand("")));

        Assert.True(ex.Errores.ContainsKey("Nombre"));
        Assert.NotEmpty(ex.Errores["Nombre"]);
    }

    [Fact]
    public async Task CreateCategoria_Valida_CreaYPersisteCategoria()
    {
        using var fixture = new ApplicationFixture();

        var id = await fixture.Mediator.Send(new CreateCategoriaCommand("Menaje"));

        Assert.True(id > 0);
        var guardada = await fixture.Uow.Categorias.ObtenerPorIdAsync(id);
        Assert.NotNull(guardada);
        Assert.Equal("Menaje", guardada.Nombre);
    }
}
