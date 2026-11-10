using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ImportNet.Domain.Entities;
namespace ImportNet.API.Models;

public sealed class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public sealed class CategoriaForm
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}
public sealed class ProductoForm
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Codigo { get; set; } = "";
    [Required, StringLength(200)] public string Nombre { get; set; } = "";
    [StringLength(1000)] public string? Descripcion { get; set; }
    [Required, StringLength(30)] public string UnidadMedida { get; set; } = "Unidad";
    [Range(typeof(decimal), "0.001", "999999999999999.999")] public decimal? Peso { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")] public decimal PrecioVenta { get; set; }
    [StringLength(20)] public string? PartidaArancelaria { get; set; }
    [Range(1, int.MaxValue)] public int CategoriaId { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.999")] public decimal StockMinimo { get; set; }
    [ValidateNever] public IReadOnlyList<Categoria> Categorias { get; set; } = [];
}
