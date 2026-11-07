const { test } = require("node:test");
const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { runInNewContext } = require("node:vm");
const source = readFileSync(join(__dirname, "../ImportNet.API/wwwroot/js/swagger-auth.js"), "utf8");

function interceptor(fetch) {
    const window = { location: { origin: "http://localhost:5080" } };
    runInNewContext(source, { window, URL, fetch });
    return window.importNetSwaggerRequest;
}

test("GET usa cookies de la sesión sin pedir CSRF", async () => {
    const execute = interceptor(() => assert.fail("No debe pedir token para GET"));
    const result = await execute({ url: "/api/productos", method: "GET" });
    assert.equal(result.credentials, "same-origin");
    assert.equal(result.headers, undefined);
});

for (const method of ["POST", "PUT", "DELETE"]) {
    test(`${method} obtiene un token nuevo y lo adjunta`, async () => {
        let calls = 0;
        const execute = interceptor(async (url, options) => {
            calls++;
            assert.equal(url, "/cuenta/csrf");
            assert.equal(options.credentials, "same-origin");
            assert.equal(options.cache, "no-store");
            return { ok: true, json: async () => ({ token: `transient-${calls}` }) };
        });
        const first = await execute({ url: "/api/categorias", method, headers: { Accept: "application/json" } });
        const second = await execute({ url: "/api/categorias", method });
        assert.equal(first.headers["X-CSRF-TOKEN"], "transient-1");
        assert.equal(second.headers["X-CSRF-TOKEN"], "transient-2");
        assert.equal(first.headers.Accept, "application/json");
    });
}

test("Sin sesión no envía una mutación", async () => {
    const execute = interceptor(async () => ({ ok: false }));
    await assert.rejects(execute({ url: "/api/productos", method: "POST" }), /Inicia sesión/);
});

test("No filtra tokens hacia otro origen ni rutas ajenas a la API", async () => {
    const execute = interceptor(() => assert.fail("No debe pedir ni adjuntar un token"));
    for (const url of ["https://example.test/api/productos", "/cuenta/logout"]) {
        const request = { url, method: "POST" };
        assert.equal(await execute(request), request);
        assert.equal(request.headers, undefined);
    }
});
