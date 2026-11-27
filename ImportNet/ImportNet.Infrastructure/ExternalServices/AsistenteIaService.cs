using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImportNet.Application.Interfaces;
using ImportNet.Application.UseCases.AsistenteIa;
using ImportNet.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImportNet.Infrastructure.ExternalServices;

/// <summary>
/// Adaptador de salida hacia proveedores externos de IA generativa (Google Gemini / OpenAI).
/// </summary>
internal sealed class AsistenteIaService(
    HttpClient httpClient,
    IOptions<IaSettings> options,
    ILogger<AsistenteIaService> logger) : IAsistenteIaService
{
    private const string ModeloGeminiPorDefecto = "gemini-flash-latest";
    private const string ModeloOpenAiPorDefecto = "gpt-4o-mini";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<RespuestaIaDto> AnalizarNegocioAsync(
        ContextoNegocioIa contexto,
        string? pregunta,
        CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new ServicioExternoException("El servicio de IA no está configurado (defina Ia:ApiKey / Ia__ApiKey).");

        var proveedor = string.IsNullOrWhiteSpace(settings.Provider) ? "Gemini" : settings.Provider.Trim();
        var modelo = string.IsNullOrWhiteSpace(settings.Model)
            ? (string.Equals(proveedor, "OpenAI", StringComparison.OrdinalIgnoreCase)
                ? ModeloOpenAiPorDefecto
                : ModeloGeminiPorDefecto)
            : settings.Model.Trim();

        var systemPrompt = """
            Eres un analista senior de retail e importaciones para la tienda ImportNet (moneda en PEN S/).
            Analiza las métricas recibidas y responde EXCLUSIVAMENTE un objeto JSON válido (sin markdown ni texto extra) con esta estructura:
            {
              "resumenEjecutivo": "string breve y accionable en español (2-3 oraciones)",
              "recomendaciones": ["3 a 5 recomendaciones concretas basadas en los datos"],
              "alertasPrioritarias": ["alertas críticas de stock bajo o margen bajo"],
              "respuestaPregunta": "respuesta directa si hubo pregunta del usuario, o null si no hubo"
            }
            """;

        var userPayload = JsonSerializer.Serialize(new
        {
            periodo = new { contexto.Desde, contexto.Hasta },
            dashboard = contexto.Dashboard,
            topProductos = contexto.TopProductos.Take(10),
            margenProductos = contexto.MargenProductos.Take(10),
            stockBajo = contexto.StockBajo.Take(15),
            preguntaUsuario = pregunta
        }, JsonOptions);

        string contenidoCrudo;
        string modeloUsado;
        try
        {
            if (string.Equals(proveedor, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                contenidoCrudo = await InvocarOpenAiAsync(settings, modelo, systemPrompt, userPayload, ct);
                modeloUsado = modelo;
            }
            else
            {
                (contenidoCrudo, modeloUsado) = await InvocarGeminiAsync(settings, modelo, systemPrompt, userPayload, ct);
            }
        }
        catch (ServicioExternoException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Tiempo de espera agotado al consultar proveedor IA {Provider}.", proveedor);
            throw new ServicioExternoException("El servicio de IA tardó demasiado en responder. Intente nuevamente.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error de red al consultar proveedor IA {Provider}.", proveedor);
            throw new ServicioExternoException("No se pudo conectar con el servicio externo de IA.", ex);
        }

        return ParsearRespuesta(contenidoCrudo, proveedor, modeloUsado);
    }

    private async Task<(string Contenido, string ModeloUsado)> InvocarGeminiAsync(
        IaSettings settings,
        string modelo,
        string systemPrompt,
        string userPayload,
        CancellationToken ct)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : settings.BaseUrl.TrimEnd('/');

        var apiKey = settings.ApiKey.Trim();
        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = $"Datos de ImportNet:\n{userPayload}" } }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        var (status, body) = await EnviarGeminiAsync(baseUrl, modelo, apiKey, payload, ct);
        var modeloUsado = modelo;

        if (EsErrorRecuperableDeModelo(status))
        {
            var alternativos = await DescubrirModelosGeminiAsync(baseUrl, apiKey, modelo, ct);
            foreach (var alternativo in alternativos)
            {
                logger.LogWarning(
                    "Modelo Gemini {ModeloConfigurado} devolvió HTTP {StatusCode}; probando modelo alternativo {ModeloAlternativo}.",
                    modeloUsado,
                    (int)status,
                    alternativo);

                modeloUsado = alternativo;
                (status, body) = await EnviarGeminiAsync(baseUrl, alternativo, apiKey, payload, ct);

                if ((int)status is >= 200 and < 300 || !EsErrorRecuperableDeModelo(status))
                    break;
            }
        }

        if ((int)status is < 200 or >= 300)
        {
            logger.LogError("Gemini respondió HTTP {StatusCode}: {Body}", (int)status, body);
            throw new ServicioExternoException(FormatearErrorExterno(status, body));
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return (text ?? throw new ServicioExternoException("La respuesta de IA no incluyó contenido."), modeloUsado);
    }

    private async Task<(HttpStatusCode Status, string Body)> EnviarGeminiAsync(
        string baseUrl,
        string modelo,
        string apiKey,
        object payload,
        CancellationToken ct)
    {
        var url = $"{baseUrl}/v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, body);
    }

    private static bool EsErrorRecuperableDeModelo(HttpStatusCode status)
        => status is HttpStatusCode.NotFound
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable;

    private async Task<IReadOnlyList<string>> DescubrirModelosGeminiAsync(
        string baseUrl,
        string apiKey,
        string modeloActual,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1beta/models");
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            return [];

        var candidatos = new List<string>();
        foreach (var item in models.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameProp))
                continue;

            var nombre = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(nombre))
                continue;

            const string prefijo = "models/";
            if (nombre.StartsWith(prefijo, StringComparison.Ordinal))
                nombre = nombre[prefijo.Length..];

            if (string.Equals(nombre, modeloActual, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!SoportaGenerateContent(item))
                continue;

            if (!nombre.Contains("gemini", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("embedding", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("image", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("tts", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("audio", StringComparison.OrdinalIgnoreCase)
                || nombre.Contains("live", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            candidatos.Add(nombre);
        }

        return candidatos
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(m => m.Contains("flash-lite", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m.Contains("flash", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => string.Equals(m, ModeloGeminiPorDefecto, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(m => m, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static bool SoportaGenerateContent(JsonElement modelElement)
    {
        if (!modelElement.TryGetProperty("supportedGenerationMethods", out var methods)
            || methods.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var m in methods.EnumerateArray())
        {
            if (string.Equals(m.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<string> InvocarOpenAiAsync(
        IaSettings settings,
        string modelo,
        string systemPrompt,
        string userPayload,
        CancellationToken ct)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://api.openai.com"
            : settings.BaseUrl.TrimEnd('/');

        var url = $"{baseUrl}/v1/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        request.Content = JsonContent.Create(new
        {
            model = modelo,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Datos de ImportNet:\n{userPayload}" }
            }
        });

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenAI respondió HTTP {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new ServicioExternoException(FormatearErrorExterno(response.StatusCode, body));
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? throw new ServicioExternoException("La respuesta de IA no incluyó contenido.");
    }

    private static string FormatearErrorExterno(HttpStatusCode status, string body)
    {
        var detalle = ExtraerMensajeError(body);
        return string.IsNullOrWhiteSpace(detalle)
            ? $"El servicio externo de IA respondió con estado {(int)status}."
            : $"El servicio externo de IA respondió con estado {(int)status}: {detalle}";
    }

    private static string? ExtraerMensajeError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("message", out var msg))
            {
                var texto = msg.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(texto))
                    return texto.Length <= 200 ? texto : texto[..200] + "…";
            }
        }
        catch (JsonException)
        {
            // Ignorar cuerpo no JSON
        }

        return null;
    }

    private RespuestaIaDto ParsearRespuesta(string crudo, string proveedor, string modelo)
    {
        var limpio = crudo.Trim();
        if (limpio.StartsWith("```", StringComparison.Ordinal))
        {
            var inicio = limpio.IndexOf('{');
            var fin = limpio.LastIndexOf('}');
            if (inicio >= 0 && fin > inicio)
                limpio = limpio[inicio..(fin + 1)];
        }

        try
        {
            var payload = JsonSerializer.Deserialize<IaJsonPayload>(limpio, JsonOptions)
                ?? throw new ServicioExternoException("No se pudo interpretar la respuesta estructurada de IA.");

            return new RespuestaIaDto(
                string.IsNullOrWhiteSpace(payload.ResumenEjecutivo)
                    ? "Análisis generado sin resumen ejecutivo."
                    : payload.ResumenEjecutivo.Trim(),
                payload.Recomendaciones ?? [],
                payload.AlertasPrioritarias ?? [],
                string.IsNullOrWhiteSpace(payload.RespuestaPregunta) ? null : payload.RespuestaPregunta.Trim(),
                proveedor,
                modelo);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON inválido recibido desde proveedor IA {Provider}: {Raw}", proveedor, limpio);
            throw new ServicioExternoException("El proveedor de IA devolvió un formato inesperado.", ex);
        }
    }

    private sealed class IaJsonPayload
    {
        [JsonPropertyName("resumenEjecutivo")]
        public string? ResumenEjecutivo { get; set; }

        [JsonPropertyName("recomendaciones")]
        public List<string>? Recomendaciones { get; set; }

        [JsonPropertyName("alertasPrioritarias")]
        public List<string>? AlertasPrioritarias { get; set; }

        [JsonPropertyName("respuestaPregunta")]
        public string? RespuestaPregunta { get; set; }
    }
}
