using ImportNet.Application;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using ImportNet.API.Configuration;
using ImportNet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddAuthorization(o => {
    o.AddPolicy("CatalogoLeer", p => p.RequireRole("Administrador", "Vendedor"));
    o.AddPolicy("CatalogoEditar", p => p.RequireRole("Administrador"));
    o.AddPolicy("Administracion", p => p.RequireRole("Administrador"));
    o.AddPolicy("Inventario", p => p.RequireRole("Administrador"));
    o.AddPolicy("Ventas", p => p.RequireRole("Administrador", "Vendedor"));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErroresHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => {
    o.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo {
        Title = "ImportNet API", Version = "v1",
        Description = "Ejecute POST /api/auth/login y copie accessToken en Authorize. " +
            "Catálogo: Administrador modifica, Vendedor consulta. Empleados y roles: solo Administrador."
    });
    o.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Pegue accessToken sin el prefijo Bearer."
    });
    o.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    o.DocInclusionPredicate((_, description) => description.RelativePath?.StartsWith("api/", StringComparison.Ordinal) == true);
});
var app = builder.Build();
if (args.Contains("--inspect-database"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Inspección habilitada exclusivamente en desarrollo.");
    try { await DatabaseInspection.Ejecutar(app.Services); }
    catch { Console.Error.WriteLine("No se pudo inspeccionar PostgreSQL. Revise User Secrets, base ImportNet y usuario postgres. No se modificó la base."); Environment.ExitCode = 1; }
    return;
}
if (args.Contains("--bootstrap-admin"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Bootstrap habilitado exclusivamente en desarrollo.");
    await AdminBootstrapper.Ejecutar(app.Services, app.Configuration);
    Console.WriteLine("Administrador y roles creados. Retire Bootstrap:Password de los secretos.");
    return;
}
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "ImportNet v1");
        o.DocumentTitle = "ImportNet · Swagger";
    });
}
else { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program;
