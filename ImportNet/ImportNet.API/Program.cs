using ImportNet.Application;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using ImportNet.API.Configuration;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddAuthorization(o => {
    o.AddPolicy("CatalogoLeer", p => p.RequireRole("Administrador", "Almacenero", "Vendedor"));
    o.AddPolicy("CatalogoEditar", p => p.RequireRole("Administrador", "Almacenero"));
    o.AddPolicy("Administracion", p => p.RequireRole("Administrador"));
    o.AddPolicy("Inventario", p => p.RequireRole("Administrador", "Almacenero"));
    o.AddPolicy("Ventas", p => p.RequireRole("Administrador", "Vendedor"));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErroresHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
if (args.Contains("--bootstrap-admin"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Bootstrap habilitado exclusivamente en desarrollo.");
    await AdminBootstrapper.Ejecutar(app.Services, app.Configuration);
    Console.WriteLine("Administrador y roles creados. Retire Bootstrap:Password de los secretos.");
    return;
}
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Inicio}/{action=Index}/{id?}");
app.Run();
public partial class Program;
