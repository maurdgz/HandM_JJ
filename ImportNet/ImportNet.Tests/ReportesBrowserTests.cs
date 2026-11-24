using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class ReportesBrowserTests
{
    [BrowserFact, Trait("Browser", "Chrome")]
    public async Task Astro_AplicaFiltrosYDescargaLasCifrasDeLaTabla()
    {
        using var factory = new SmokeFactory(useKestrel: true);
        using var client = TestSupport.CreateKestrelClient(factory);
        factory.InitializeSchema();
        string email = "", password = "";
        await SmokeTests.Login(factory, client, "Administrador", (e, p) => (email, password) = (e, p));
        await ReportesExcelTests.Sembrar(factory, client);

        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_reportes_browser.py"));
        var start = new ProcessStartInfo("python")
        {
            UseShellExecute = false, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(script);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar Playwright.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { url = client.BaseAddress, email, password }));
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Tiempo agotado validando Reportes en Chrome.");
        }
        await Task.WhenAll(output, errors);
        var diagnostico = (await errors).Split('\n').FirstOrDefault(x => x.StartsWith("Reportes browser:", StringComparison.Ordinal));
        Assert.True(process.ExitCode == 0, diagnostico ?? "Falló la prueba de navegador de Reportes.");
        Console.WriteLine(await output);
    }
}
