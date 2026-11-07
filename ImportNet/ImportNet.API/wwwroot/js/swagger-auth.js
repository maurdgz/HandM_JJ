// Uses the existing same-origin Identity session. No credentials or tokens are persisted.
window.importNetSwaggerRequest = async function (request) {
    const target = new URL(request.url, window.location.origin);
    if (target.origin !== window.location.origin || !target.pathname.startsWith("/api/")) {
        return request;
    }
    request.credentials = "same-origin";
    const method = (request.method || "GET").toUpperCase();
    if (["GET", "HEAD", "OPTIONS"].includes(method)) {
        return request;
    }
    const response = await fetch("/cuenta/csrf", {
        credentials: "same-origin",
        cache: "no-store",
        headers: { Accept: "application/json" }
    });
    if (!response.ok) {
        throw new Error("Inicia sesión desde el enlace de ImportNet y vuelve a Swagger. No se envió la operación.");
    }
    const { token } = await response.json();
    if (!token) {
        throw new Error("No se obtuvo el token CSRF. Recarga Swagger después de iniciar sesión.");
    }
    request.headers = request.headers || {};
    request.headers["X-CSRF-TOKEN"] = token;
    return request;
};
