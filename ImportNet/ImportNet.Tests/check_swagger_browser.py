"""Optional Swagger JWT browser check. Credentials arrive only via stdin; no traces or screenshots."""
import argparse
import json
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
            page.locator(".opblock").first.wait_for()
            page.locator(".auth-wrapper button.authorize").wait_for()

            def execute(method, path, status, body=None, identifier=None):
                global stage
                stage = method + " " + path
                block = page.locator(".opblock-" + method.lower()).filter(
                    has=page.locator(f'.opblock-summary-path[data-path="{path}"]'))
                if not block.locator(".opblock-body").is_visible():
                    block.locator(".opblock-summary").click()
                block.locator(".try-out__btn").click()
                if body is not None:
                    block.locator("textarea.body-param__text").fill(json.dumps(body))
                actual_path = path
                if identifier is not None:
                    row = block.locator("tr").filter(has=page.locator(".parameter__name", has_text="id"))
                    row.locator("input").fill(str(identifier))
                    actual_path = path.replace("{id}", str(identifier))
                with page.expect_response(lambda r: urlparse(r.url).path == actual_path and r.request.method == method) as pending:
                    block.get_by_role("button", name="Execute", exact=True).click()
                response = pending.value
                assert response.status == status
                return response.json() if status in (200, 201) else None

            if authenticated:
                token = execute("POST", "/api/auth/login", 200, credentials)["accessToken"]
                stage = "Authorize Bearer"
                page.locator(".auth-wrapper button.authorize").click()
                page.locator(".dialog-ux input").fill(token)
                page.locator(".dialog-ux button.authorize").click()
                page.locator(".dialog-ux button.btn-done").click()
                me = execute("GET", "/api/auth/me", 200)
                assert "Administrador" in me["roles"] and me["empleadoId"] > 0
                execute("GET", "/api/empleados", 200)
                unique = uuid.uuid4().hex
                category = execute("POST", "/api/categorias", 201, {"nombre": "Browser-" + unique})["id"]
                product = execute("POST", "/api/productos", 201, {
                    "codigo": "B-" + unique, "nombre": "Browser JWT", "unidadMedida": "Unidad",
                    "precioVenta": 12.50, "categoriaId": category, "stockMinimo": 1})["id"]
                execute("GET", "/api/productos/{id}", 200, identifier=product)
                execute("DELETE", "/api/productos/{id}", 204, identifier=product)
                execute("DELETE", "/api/categorias/{id}", 204, identifier=category)
            print("Swagger Bearer browser verification passed")
        finally:
            browser.close()

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", required=True)
    parser.add_argument("--authenticated", action="store_true")
    args = parser.parse_args()
    try:
        check(args.url.rstrip("/"), args.authenticated)
    except Exception:
        print("Browser verification failed at " + stage, file=sys.stderr)
        sys.exit(1)
