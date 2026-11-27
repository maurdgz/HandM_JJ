using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class Categoria : Entity
{
    public string Nombre { get; private set; } = "";
    public string NombreNormalizado { get; private set; } = "";

    private Categoria()
    {
    }

    public Categoria(string nombre)
    {
        Editar(nombre);
    }

    public void Editar(string nombre)
    {
        Nombre = Reglas.Texto(nombre, 100, "Nombre");
        NombreNormalizado = Nombre.ToUpperInvariant();
    }
}
