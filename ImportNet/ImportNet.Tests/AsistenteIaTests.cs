using System.Net;
using System.Text;
using System.Text.Json;
using ImportNet.Application.Interfaces;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.AsistenteIa;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ImportNet.Tests;

public sealed class AsistenteIaTests
{
    [Fact]
    public async Task AsistenteIaService_SinApiKey_LanzaServicioExternoException()
    {
        var service = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.OK), new IaSettings { ApiKey = "" });

        await Assert.ThrowsAsync<ServicioExternoException>(() =>
            service.AnalizarNegocioAsync(CrearContexto(), null));
    }

    [Fact]
    public async Task AsistenteIaService_GeminiRespuestaConMarkdown_ParseaCorrectamente()
    {
        var jsonGemini = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = """
                                    ```json
                                    {
                                      "resumenEjecutivo": "Ventas sólidas en el periodo.",
                                      "recomendaciones": ["Reponer Olla 24cm"],
                                      "alertasPrioritarias": ["Stock crítico en Olla 24cm"],
                                      "respuestaPregunta": "Prioriza Olla 24cm."
                                    }
                                    ```
                                    """
                            }
                        }
                    }
                }
            }
        });

        string? headerKey = null;
        var service = CrearServicio(req =>
        {
            headerKey = req.Headers.TryGetValues("x-goog-api-key", out var vals) ? vals.FirstOrDefault() : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonGemini, Encoding.UTF8, "application/json")
            };
        }, new IaSettings { Provider = "Gemini", ApiKey = "test-gemini-key", Model = "gemini-2.5-flash" });

        var res = await service.AnalizarNegocioAsync(CrearContexto(), "¿Qué reponer?");

        Assert.Equal("test-gemini-key", headerKey);
        Assert.Equal("Ventas sólidas en el periodo.", res.ResumenEjecutivo);
        Assert.Single(res.Recomendaciones);
        Assert.Single(res.AlertasPrioritarias);
        Assert.Equal("Prioriza Olla 24cm.", res.RespuestaPregunta);
        Assert.Equal("Gemini", res.ProveedorIa);
    }

    [Fact]
    public async Task AsistenteIaService_Gemini404_DescubreModeloDisponibleYReintenta()
    {
        var jsonModels = JsonSerializer.Serialize(new
        {
            models = new object[]
            {
                new
                {
                    name = "models/gemini-3.5-flash",
                    supportedGenerationMethods = new[] { "generateContent" }
                }
            }
        });

        var jsonGemini = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = """
                                    {
                                      "resumenEjecutivo": "Recuperado con modelo alternativo.",
                                      "recomendaciones": ["Mantener stock"],
                                      "alertasPrioritarias": [],
                                      "respuestaPregunta": null
                                    }
                                    """
                            }
                        }
                    }
                }
            }
        });

        var service = CrearServicio(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/models/gemini-2.5-flash:generateContent", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"Model not found\"}}", Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/v1beta/models", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonModels, Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/models/gemini-3.5-flash:generateContent", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonGemini, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }, new IaSettings { Provider = "Gemini", ApiKey = "test-key", Model = "gemini-2.5-flash" });

        var res = await service.AnalizarNegocioAsync(CrearContexto(), null);

        Assert.Equal("Recuperado con modelo alternativo.", res.ResumenEjecutivo);
        Assert.Equal("gemini-3.5-flash", res.Modelo);
    }

    [Fact]
    public async Task AsistenteIaService_Gemini503_HaceFailoverAModeloDisponible()
    {
        var jsonModels = JsonSerializer.Serialize(new
        {
            models = new object[]
            {
                new { name = "models/gemini-3.5-flash-lite", supportedGenerationMethods = new[] { "generateContent" } }
            }
        });

        var jsonGemini = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = """
                                    {
                                      "resumenEjecutivo": "Failover tras 503 exitoso.",
                                      "recomendaciones": ["Revisar stock"],
                                      "alertasPrioritarias": [],
                                      "respuestaPregunta": null
                                    }
                                    """
                            }
                        }
                    }
                }
            }
        });

        var service = CrearServicio(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/models/gemini-flash-latest:generateContent", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"This model is currently experiencing high demand.\"}}", Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/v1beta/models", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonModels, Encoding.UTF8, "application/json")
                };
            }

            if (path.EndsWith("/models/gemini-3.5-flash-lite:generateContent", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonGemini, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }, new IaSettings { Provider = "Gemini", ApiKey = "test-key", Model = "gemini-flash-latest" });

        var res = await service.AnalizarNegocioAsync(CrearContexto(), null);

        Assert.Equal("Failover tras 503 exitoso.", res.ResumenEjecutivo);
        Assert.Equal("gemini-3.5-flash-lite", res.Modelo);
    }

    [Fact]
    public async Task AsistenteIaService_OpenAiError500_LanzaServicioExternoException()
    {
        var service = CrearServicio(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("error") },
            new IaSettings { Provider = "OpenAI", ApiKey = "sk-test", Model = "gpt-4o-mini" });

        await Assert.ThrowsAsync<ServicioExternoException>(() =>
            service.AnalizarNegocioAsync(CrearContexto(), null));
    }

    [Fact]
    public async Task AnalizarNegocioIaCommandHandler_ExigeAdminYRetornaAnalisis()
    {
        var uow = new FakeUnitOfWork();
        var usuario = new FakeUsuarioActual(esAdmin: true);
        var ia = new FakeIaService();
        var handler = new AnalizarNegocioIaCommandHandler(uow, usuario, ia);

        var resultado = await handler.Handle(
            new AnalizarNegocioIaCommand(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 20), "¿Cómo va el margen?"),
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 11, 1), resultado.Desde);
        Assert.Equal(new DateOnly(2026, 11, 20), resultado.Hasta);
        Assert.Equal("Resumen IA", resultado.ResumenEjecutivo);
        Assert.Equal("Gemini", resultado.ProveedorIa);
    }

    [Fact]
    public async Task AnalizarNegocioIaCommandHandler_NoAdmin_LanzaAccesoNoPermitido()
    {
        var handler = new AnalizarNegocioIaCommandHandler(
            new FakeUnitOfWork(),
            new FakeUsuarioActual(esAdmin: false),
            new FakeIaService());

        var ex = await Assert.ThrowsAsync<AccesoNoPermitidoException>(() =>
            handler.Handle(new AnalizarNegocioIaCommand(), CancellationToken.None));

        Assert.Equal(403, ex.Status);
    }

    private static AsistenteIaService CrearServicio(Func<HttpRequestMessage, HttpResponseMessage> responder, IaSettings settings)
    {
        var client = new HttpClient(new StubHandler(responder));
        return new AsistenteIaService(client, Options.Create(settings), NullLogger<AsistenteIaService>.Instance);
    }

    private static ContextoNegocioIa CrearContexto()
        => new(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 27),
            new ResumenDashboard(new DateOnly(2026, 11, 27), 250m, 3, 4500m, 40, 112.5m, 1200m, 15, 2, 1),
            [new TopProducto(1, "OLLA-24", "Olla 24cm", "Unidad", 12m, 1200m)],
            [new MargenProducto(1, "OLLA-24", "Olla 24cm", 12m, 1200m, 60m, 720m, 480m, 40m)],
            [new ProductoStockBajo(1, "OLLA-24", "Olla 24cm", "Unidad", 1m, 5m, 4m)]);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class FakeUsuarioActual(bool esAdmin) : IUsuarioActual
    {
        public string? Id => "user-1";
        public Task<bool> EsAdministradorAsync(CancellationToken ct = default) => Task.FromResult(esAdmin);
        public Task ExigirAdministradorAsync(CancellationToken ct = default)
            => esAdmin ? Task.CompletedTask : throw new AccesoNoPermitidoException(403);
    }

    private sealed class FakeIaService : IAsistenteIaService
    {
        public Task<RespuestaIaDto> AnalizarNegocioAsync(ContextoNegocioIa contexto, string? pregunta, CancellationToken ct = default)
            => Task.FromResult(new RespuestaIaDto("Resumen IA", ["Rec 1"], ["Alerta 1"], "Respuesta", "Gemini", "gemini-2.5-flash"));
    }

    private sealed class FakeReporteRepository : IReporteRepository
    {
        public Task<ResumenDashboard> DashboardAsync(DateOnly fecha, DateTime inicioDiaUtc, DateTime finDiaUtc, DateTime inicioMesUtc, CancellationToken ct = default)
            => Task.FromResult(new ResumenDashboard(fecha, 100m, 1, 1000m, 10, 100m, 300m, 5, 1, 0));

        public Task<List<TopProducto>> TopProductosAsync(DateTime desdeUtc, DateTime hastaUtc, int top, CancellationToken ct = default)
            => Task.FromResult(new List<TopProducto>());

        public Task<List<MargenProducto>> MargenPorProductoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(new List<MargenProducto>());

        public Task<List<ProductoStockBajo>> StockBajoMinimoAsync(CancellationToken ct = default)
            => Task.FromResult(new List<ProductoStockBajo>());

        public Task<List<VentasPorPeriodo>> VentasPorPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, AgrupacionPeriodo agrupacion, CancellationToken ct = default)
            => Task.FromResult(new List<VentasPorPeriodo>());

        public Task<List<VentasPorVendedor>> VentasPorVendedorAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(new List<VentasPorVendedor>());

        public Task<CierreCaja> CierreCajaAsync(DateOnly fecha, DateTime desdeUtc, DateTime hastaUtc, int? empleadoId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<List<ComprasPorProveedor>> ComprasPorProveedorAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            => Task.FromResult(new List<ComprasPorProveedor>());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public IReporteRepository Reportes { get; } = new FakeReporteRepository();
        public ICompraRepository Compras => throw new NotImplementedException();
        public IClienteRepository Clientes => throw new NotImplementedException();
        public IProveedorRepository Proveedores => throw new NotImplementedException();
        public ICategoriaRepository Categorias => throw new NotImplementedException();
        public IProductoRepository Productos => throw new NotImplementedException();
        public IInventarioRepository Inventarios => throw new NotImplementedException();
        public IMovimientoRepository Movimientos => throw new NotImplementedException();
        public IVentaRepository Ventas => throw new NotImplementedException();
        public IEmpleadoRepository Empleados => throw new NotImplementedException();
        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
            => throw new NotImplementedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
