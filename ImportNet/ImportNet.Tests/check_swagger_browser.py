"""Optional real-browser verification. Credentials arrive only through stdin; no traces or screenshots."""
import argparse
import json
import re
import sys
import uuid
from urllib.parse import urlparse
from playwright.sync_api import sync_playwright

stage = "startup"

def check(base_url, authenticated):
    global stage
    if urlparse(base_url).hostname not in ("localhost", "127.0.0.1", "::1"):
        raise ValueError("Only loopback verification is allowed")
    credentials = json.load(sys.stdin) if authenticated else None
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(channel="chrome", headless=True)
        try:
            page = browser.new_page()
            page.set_default_timeout(20000)
            page.goto(base_url + "/swagger/index.html")
            page.wait_for_function("typeof window.importNetSwaggerRequest === 'function' && window.ui")
            page.locator(".opblock").first.wait_for()
            assert page.locator(".opblock").count() >= 10
            assert page.locator('.opblock-summary-path[data-path="/api/categorias"]').count() == 2
            assert page.locator('.opblock-summary-path[data-path="/api/productos"]').count() == 2
            blocked = page.evaluate("""async () => {
                try { await window.importNetSwaggerRequest({url:'/api/categorias', method:'POST'}); return false; }
                catch { return true; }
            }""")
            assert blocked
            checks = 3
            if authenticated:
                stage = "login"
                page.goto(base_url + "/cuenta/login?returnUrl=%2Fswagger")
                page.locator('input[name="Email"]').fill(credentials["email"])
                page.locator('input[name="Password"]').fill(credentials["password"])
                page.get_by_role("button", name="Iniciar sesión").click()
                page.wait_for_url(re.compile(r"/swagger(?:/index.html)?$"))
                page.locator(".opblock").first.wait_for()

                def execute(method, path, status, body=None, identifier=None):
                    nonlocal checks
                    global stage
                    operation = method + " " + path
                    stage = operation + " (select)"
                    block = page.locator(".opblock").filter(
                        has=page.locator(f'.opblock-summary-path[data-path="{path}"]')
                    ).filter(has=page.locator(".opblock-summary-method", has_text=re.compile(f"^{method}$")))
                    assert block.count() == 1
                    stage = operation + " (expand)"
                    if not block.locator(".opblock-body").is_visible():
                        block.locator(".opblock-summary").click()
                    block.locator(".opblock-body").wait_for(state="visible")
                    stage = operation + " (try it out)"
                    try_button = block.get_by_role("button", name="Try it out", exact=True)
                    if not block.get_by_role("button", name="Cancel", exact=True).is_visible():
                        try_button.click()
                    if identifier is not None:
                        stage = operation + " (path parameter)"
                        block.locator('input[placeholder="id"]').fill(str(identifier))
                    if body is not None:
                        stage = operation + " (request body)"
                        block.locator("textarea.body-param__text").fill(json.dumps(body))
                    target = base_url + path.replace("{id}", str(identifier))
                    stage = operation + " (execute)"
                    with page.expect_response(lambda r: r.url == target and r.request.method == method) as pending:
                        block.get_by_role("button", name="Execute", exact=True).click()
                    response = pending.value
                    headers = response.request.all_headers()
                    body_matches = body is None or response.request.post_data_json == body
                    stage = operation + f" (HTTP {response.status}; expected {status}; CSRF header: {'x-csrf-token' in headers}; JSON: {'application/json' in headers.get('content-type', '')}; body match: {body_matches})"
                    assert response.status == status, f"Unexpected HTTP status for {method} {path}"
                    checks += 1
                    return response.json() if status == 201 else None

                unique = uuid.uuid4().hex
                categoria = execute("POST", "/api/categorias", 201, {"nombre": "Browser-" + unique})["id"]
                execute("GET", "/api/categorias", 200)
                execute("GET", "/api/categorias/{id}", 200, identifier=categoria)
                execute("PUT", "/api/categorias/{id}", 204, {"id": categoria, "nombre": "Edit-" + unique}, categoria)
                product = {"codigo": "B-" + unique, "nombre": "Producto navegador", "unidadMedida": "Unidad", "peso": 0.125,
                           "precioUsd": 4.50, "partidaArancelaria": "854442", "categoriaId": categoria, "stockMinimo": 1}
                producto = execute("POST", "/api/productos", 201, product)["id"]
                execute("GET", "/api/productos", 200)
                execute("GET", "/api/productos/{id}", 200, identifier=producto)
                execute("PUT", "/api/productos/{id}", 204, {**product, "id": producto, "precioUsd": 5.25}, producto)
                execute("DELETE", "/api/categorias/{id}", 409, identifier=categoria)
                execute("DELETE", "/api/productos/{id}", 204, identifier=producto)
                execute("DELETE", "/api/categorias/{id}", 204, identifier=categoria)
                execute("GET", "/api/empleados", 200)
                stage = "empleados MVC registro"
                page.goto(base_url + "/empleados/crear")
                documento = "BR" + unique[:12].upper()
                fields = {"Nombre": "Persona", "Apellidos": "Navegador", "DocumentoIdentidad": documento,
                          "Email": unique + "@example.test", "Telefono": "+51987654321", "Cargo": "Vendedor", "Area": "Comercial",
                          "Password": "Temp!aA1" + uuid.uuid4().hex}
                for field, value in fields.items():
                    page.locator(f'input[name="{field}"]').fill(value)
                page.locator("#role-Vendedor").check()
                page.get_by_role("button", name="Guardar empleado", exact=True).click()
                page.wait_for_url(re.compile(r"/empleados$"))
                row = page.locator("tbody tr").filter(has_text=documento)
                assert row.count() == 1
                checks += 1
                stage = "empleados MVC edicion"
                row.get_by_role("link", name="Editar", exact=True).click()
                page.locator('input[name="Apellidos"]').fill("Editado")
                page.locator("#role-Vendedor").uncheck()
                page.locator("#role-Administrador").check()
                page.get_by_role("button", name="Guardar empleado", exact=True).click()
                page.wait_for_url(re.compile(r"/empleados$"))
                row = page.locator("tbody tr").filter(has_text=documento)
                assert "Editado" in row.inner_text() and "Administrador" in row.inner_text()
                checks += 1
                stage = "empleados MVC desactivacion"
                with page.expect_navigation():
                    row.get_by_role("button", name="Desactivar", exact=True).click()
                saved = page.evaluate("async doc => (await fetch('/api/empleados?busqueda=' + encodeURIComponent(doc))).json()", documento)
                assert len(saved) == 1 and saved[0]["activo"] is False
                checks += 1
                page.goto(base_url + "/")
                stage = "logout"
                page.get_by_role("button", name="Cerrar sesión").click()
                page.wait_for_url(re.compile(r"/cuenta/login$"))
                assert page.evaluate("async () => (await fetch('/api/productos')).status") == 401
                checks += 1
            print(json.dumps({"browser": "Chrome headless", "authenticated": authenticated, "checks": checks, "result": "passed"}))
        finally:
            browser.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", default="http://localhost:5080")
    parser.add_argument("--authenticated", action="store_true")
    args = parser.parse_args()
    try:
        check(args.url.rstrip("/"), args.authenticated)
    except Exception as error:
        print("Browser verification failed at " + stage + " [" + type(error).__name__ + "]. No credentials, traces or request bodies were logged.", file=sys.stderr)
        sys.exit(1)
