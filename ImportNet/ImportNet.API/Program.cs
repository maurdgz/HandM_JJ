using ImportNet.API.Configuration;
using ImportNet.Application;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Identity;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();

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
    options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<ErroresHandler>();
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
});

var app = builder.Build();

if (args.Contains("--bootstrap-admin"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Bootstrap habilitado exclusivamente en desarrollo.");

    await AdminBootstrapper.Ejecutar(app.Services, app.Configuration);
    Console.WriteLine("Administrador y roles creados. Retire Bootstrap:Password de los secretos.");
    return;
}

app.Services.GetRequiredService<JwtSettings>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseApiResponses();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
