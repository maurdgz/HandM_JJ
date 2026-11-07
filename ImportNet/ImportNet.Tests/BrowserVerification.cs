using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IMPORTNET_BROWSER_TESTS") != "1")
            Skip = "Requiere IMPORTNET_BROWSER_TESTS=1, Python Playwright y Chrome local.";
    }
}

internal static class BrowserVerification
{
    public static async Task Ejecutar(Uri baseUrl, string email, string password)
    {
        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_swagger_browser.py"));
        var start = new ProcessStartInfo("python") {
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var argument in new[] { script, "--url", baseUrl.ToString().TrimEnd('/'), "--authenticated" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar la prueba de navegador.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { email, password }));
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("La prueba de Swagger en navegador excedió el tiempo permitido."); }
        await Task.WhenAll(output, errors);
        var safeDiagnostic = (await errors).Split('\n').FirstOrDefault(x => x.StartsWith("Browser verification failed at ", StringComparison.Ordinal));
        Assert.True(process.ExitCode == 0, "Falló la prueba de Swagger en navegador. " + safeDiagnostic);
        Console.WriteLine(await output);
    }
}
