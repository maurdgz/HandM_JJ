namespace ImportNet.Domain.Common;

/// <summary>
/// Clase base para todas las entidades persistentes del dominio.
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }
}
