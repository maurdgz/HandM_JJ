using ImportNet.API.Configuration;
using ImportNet.API.Middleware;
using ImportNet.Application;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port)
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogoLeer", p => p.RequireRole("Administrador", "Vendedor"));
    options.AddPolicy("CatalogoEditar", p => p.RequireRole("Administrador"));
    options.AddPolicy("Administracion", p => p.RequireRole("Administrador"));
    options.AddPolicy("Inventario", p => p.RequireRole("Administrador"));
    options.AddPolicy("Ventas", p => p.RequireRole("Administrador", "Vendedor"));
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddLoginRateLimiting(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ImportNet API",
        Version = "v1",
        Description = "Ejecute POST /api/auth/login y copie accessToken en Authorize."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pegue accessToken sin el prefijo Bearer."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.DocInclusionPredicate((_, description) =>
        description.RelativePath?.StartsWith("api/", StringComparison.Ordinal) == true);
});

var app = builder.Build();

if (args.Contains("--bootstrap-admin"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Bootstrap habilitado exclusivamente en desarrollo.");

    await AdminBootstrapper.EjecutarAsync(app.Services, app.Configuration);
    Console.WriteLine("Administrador y roles creados. Retire Bootstrap:Password de los secretos.");
    return;
}

await DatabaseStartupExtensions.AplicarMigracionesSiCorrespondeAsync(app.Services, app.Configuration, app.Logger);

// Fail closed at startup when JWT signing configuration is missing or invalid.
_ = app.Services.GetRequiredService<JwtSettings>();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ImportNet v1");
        options.DocumentTitle = "ImportNet · Swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseApiResponses();
app.UseFrontend();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
