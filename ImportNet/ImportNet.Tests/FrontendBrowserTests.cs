using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class FrontendBrowserTests
{
    [BrowserFact, Trait("Browser", "Chromium")]
    public async Task Astro_EmpleadosCrudRolesEstadosYSesionJwt()
    {
        using var factory = new SmokeFactory(useKestrel: true);
        using var client = TestSupport.CreateKestrelClient(factory);
        factory.InitializeSchema();
        var admin = await AuthApiTests.CrearUsuario(factory, "Administrador");
        var seller = await AuthApiTests.CrearUsuario(factory, "Vendedor");
        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_frontend_browser.py"));
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("IMPORTNET_PYTHON") ?? "python3")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(script);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar Playwright.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new
        {
            apiUrl = client.BaseAddress!.ToString(),
            admin = new { email = admin.Email, password = admin.Password },
            seller = new { email = seller.Email, password = seller.Password }
        }));
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Tiempo agotado en la prueba de frontend y empleados.");
        }
        await Task.WhenAll(output, errors);
        var diagnostic = (await errors).Split('\n').FirstOrDefault(x => x.StartsWith("Frontend verification failed at "));
        Assert.True(process.ExitCode == 0, "Falló la prueba del frontend. " + diagnostic);
        Console.WriteLine(await output);
    }
}
