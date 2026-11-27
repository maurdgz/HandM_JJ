using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class VentasBrowserTests
{
    [BrowserFact, Trait("Browser", "Chrome")]
    public async Task UnSoloHost_HistorialNotaImpresionAnulacionYPermisos()
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API/wwwroot"));
        Assert.True(File.Exists(Path.Combine(webRoot, "ventas/nota/index.html")), "Ejecute scripts/Preparar-Frontend.ps1 antes de la prueba Chrome.");
        using var factory = new SmokeFactory(useKestrel: true, webRoot: webRoot);
        using var adminClient = TestSupport.CreateKestrelClient(factory);
        using var sellerClient = TestSupport.CreateKestrelClient(factory);
        factory.InitializeSchema();
        string adminEmail = "", adminPassword = "", sellerEmail = "", sellerPassword = "";
        await SmokeTests.Login(factory, adminClient, "Administrador", (email, password) => (adminEmail, adminPassword) = (email, password));
        await SmokeTests.Login(factory, sellerClient, "Vendedor", (email, password) => (sellerEmail, sellerPassword) = (email, password));
        var products = await VentasDatosPrueba.Catalogo(factory.Services);
        var own = await VentasDatosPrueba.Registrar(sellerClient, products);
        var foreign = await VentasDatosPrueba.Registrar(adminClient, await VentasDatosPrueba.Catalogo(factory.Services));
        var start = new ProcessStartInfo("python") {
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../check_ventas_browser.py")));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar Chrome.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new {
            url = adminClient.BaseAddress!.ToString(),
            admin = new { email = adminEmail, password = adminPassword },
            seller = new { email = sellerEmail, password = sellerPassword },
            ownId = own.Id, ownNumber = own.Numero, foreignId = foreign.Id
        }));
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException("La prueba de ventas en Chrome excedió 180 segundos."); }
        await Task.WhenAll(output, errors);
        var diagnostic = (await errors).Split('\n').FirstOrDefault(line => line.StartsWith("Browser verification failed at ", StringComparison.Ordinal));
        Assert.True(process.ExitCode == 0, "Falló Chrome. " + (diagnostic ?? "Revise Python, Playwright y Chrome local."));
        Console.WriteLine(await output);
        await VentasDatosPrueba.ComprobarReposicion(factory.Services, own.Id, products);
    }
}
