namespace ImportNet.Domain.Interfaces;

public sealed record Pagina<T>(IReadOnlyList<T> Items, int Count, int PaginaActual, int Tamano);
