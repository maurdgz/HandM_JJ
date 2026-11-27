using System.Net;
using Xunit;

namespace ImportNet.Tests;

public sealed class FrontendHostingTests
{
    [Fact]
    public async Task UnSoloHost_SirvePaginasAstroSinOcultarErroresNiExponerConfiguracion()
    {
        var root = Path.Combine(Path.GetTempPath(), "importnet-host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var route in new[] { "", "login", "ventas", "ventas/historial", "ventas/nota" })
            {
                var directory = Path.Combine(root, route);
                Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(Path.Combine(directory, "index.html"), "<!doctype html><title>ImportNet</title>");
            }
            Directory.CreateDirectory(Path.Combine(root, "_astro"));
            await File.WriteAllTextAsync(Path.Combine(root, "_astro", "test.js"), "console.log('asset');");
            using var factory = new SmokeFactory(webRoot: root);
            using var client = factory.CreateClient();
            factory.InitializeSchema();
            foreach (var route in new[] { "/", "/login/", "/ventas/", "/ventas/historial/", "/ventas/nota/?id=1" })
            {
                using var response = await client.GetAsync(route);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
                Assert.True(response.Headers.CacheControl!.NoStore);
            }
            using var asset = await client.GetAsync("/_astro/test.js");
            Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
            Assert.Equal(TimeSpan.FromDays(365), asset.Headers.CacheControl!.MaxAge);
            foreach (var route in new[] { "/api/no-existe", "/appsettings.json", "/.env", "/ImportNet.API.dll", "/ventas/no-existe/" })
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/ventas")).StatusCode);
        }
        finally
        {
            var expectedParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            Assert.Equal(expectedParent, Path.GetDirectoryName(Path.GetFullPath(root)));
            Assert.StartsWith("importnet-host-", Path.GetFileName(root));
            Directory.Delete(root, recursive: true);
        }
    }
}
