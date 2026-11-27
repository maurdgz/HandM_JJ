"""Astro build + real .NET API on one host with transient SQLite. No production data or stored credentials."""
import json
import os
import sys
import uuid
import re
from urllib.parse import urlparse
from playwright.sync_api import sync_playwright, expect

stage = "startup"
data = json.load(sys.stdin)
assert urlparse(data["apiUrl"]).hostname in ("localhost", "127.0.0.1", "::1")
url = data["apiUrl"].rstrip("/")
session_keys = ["importnet.token", "importnet.expiration", "importnet.user", "importnet.roles"]


def session_cleared(page):
    assert page.evaluate("keys => keys.every(k => sessionStorage.getItem(k) === null)", session_keys)


def response_is(method, path):
    return lambda response: response.request.method == method and urlparse(response.url).path == path


def login(page, account):
    page.goto(url + "/login")
    page.get_by_label("Correo", exact=True).fill(account["email"])
    page.get_by_label("Contraseña", exact=True).fill(account["password"])
    with page.expect_response(response_is("POST", "/api/auth/login")) as pending:
        page.get_by_role("button", name="Ingresar", exact=True).click()
    assert pending.value.status == 200
    expect(page.get_by_role("navigation", name="Principal")).to_be_visible()


try:
    with sync_playwright() as pw:
        # CI uses Playwright Chromium; an installed Chrome can be selected locally.
        browser = pw.chromium.launch(headless=True, channel=os.environ.get("IMPORTNET_BROWSER_CHANNEL", "chrome"))
        try:
            context = browser.new_context()
            page = context.new_page()
            page.set_default_timeout(20000)
            expect.set_options(timeout=20000)
            stage = "anonymous guard"
            page.goto(url + "/empleados")
            expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            session_cleared(page)

            stage = "login validation and incorrect credentials"
            page.get_by_role("button", name="Ingresar", exact=True).click()
            expect(page.get_by_text("Introduce tu contraseña.", exact=True)).to_be_visible()
            page.get_by_label("Correo", exact=True).fill(data["admin"]["email"])
            page.get_by_label("Contraseña", exact=True).fill("wrong-password")
            with page.expect_response(response_is("POST", "/api/auth/login")) as pending:
                page.get_by_role("button", name="Ingresar", exact=True).click()
            assert pending.value.status == 401
            expect(page.get_by_role("alert")).to_contain_text("Correo o contraseña incorrectos")
            session_cleared(page)

            stage = "admin login and session storage"
            login(page, data["admin"])
            assert page.evaluate("JSON.parse(sessionStorage.getItem('importnet.roles'))") == ["Administrador"]
            assert page.evaluate("localStorage.getItem('importnet.token')") is None
            page.get_by_role("link", name="Empleados", exact=True).click()
            expect(page.get_by_role("table")).to_be_visible()

            stage = "create employee validation and POST through Vite proxy"
            page.get_by_role("button", name="Nuevo empleado", exact=True).click()
            dialog = page.get_by_role("dialog")
            dialog.get_by_role("button", name="Guardar", exact=True).click()
            expect(dialog.get_by_text("Campo obligatorio.", exact=True).first).to_be_visible()
            employee_email = "browser-" + uuid.uuid4().hex + "@example.test"
            fields = {"Nombres": "Empleado navegador", "Apellidos": "Pruebas", "Documento": "98765432",
                      "Correo": employee_email, "Teléfono": "999888777", "Cargo": "Ventas", "Área": "Tienda",
                      "Contraseña inicial": "Browser-Test-2026!"}
            for label, value in fields.items():
                dialog.get_by_label(label, exact=True).fill(value)
            held_posts = []
            def pending_create(route):
                if route.request.method == "POST":
                    held_posts.append(route)
                else:
                    route.continue_()
            page.route("**/api/empleados", pending_create)
            with page.expect_request(lambda request: request.method == "POST" and urlparse(request.url).path == "/api/empleados"):
                dialog.get_by_role("button", name="Guardar", exact=True).click()
            expect(dialog.get_by_role("button", name="Guardando…", exact=True)).to_be_disabled()
            page.keyboard.press("Escape")
            expect(dialog).to_be_visible()
            dialog.get_by_role("button", name="Close", exact=True).click()
            expect(dialog).to_be_visible()
            assert len(held_posts) == 1
            with page.expect_response(response_is("POST", "/api/empleados")) as pending:
                held_posts[0].continue_()
            page.unroute("**/api/empleados", pending_create)
            assert pending.value.status == 201
            assert pending.value.request.headers.get("authorization", "").startswith("Bearer ")
            identifier = pending.value.json()["id"]
            expect(dialog).not_to_be_visible()
            row = page.get_by_role("row").filter(has_text=employee_email)
            expect(row).to_contain_text("Empleado navegador")
            expect(row).to_contain_text("Activo")

            stage = "edit employee"
            row.get_by_role("button", name="Editar", exact=True).click()
            dialog = page.get_by_role("dialog")
            dialog.get_by_label("Nombres", exact=True).fill("Empleado actualizado")
            with page.expect_response(response_is("PUT", f"/api/empleados/{identifier}")) as pending:
                dialog.get_by_role("button", name="Guardar", exact=True).click()
            assert pending.value.status == 204
            expect(page.get_by_role("row").filter(has_text=employee_email)).to_contain_text("Empleado actualizado")

            stage = "cancel and confirm soft delete"
            row = page.get_by_role("row").filter(has_text=employee_email)
            row.get_by_role("button", name="Eliminar", exact=True).click()
            page.get_by_role("dialog").get_by_role("button", name="Cancelar", exact=True).click()
            expect(row).to_contain_text("Activo")
            row.get_by_role("button", name="Eliminar", exact=True).click()
            with page.expect_response(response_is("DELETE", f"/api/empleados/{identifier}")) as pending:
                page.get_by_role("button", name="Confirmar eliminación", exact=True).click()
            assert pending.value.status == 204
            expect(page.get_by_role("row").filter(has_text=employee_email)).to_contain_text("Inactivo")
            expect(page.get_by_role("row").filter(has_text=employee_email).get_by_role("button", name="Eliminar", exact=True)).to_have_count(0)

            # Only these UI state checks stub responses; CRUD and authorization use .NET.
            stage = "loading, error with retry, and empty state"
            held_requests = []
            def pending_list(route):
                held_requests.append(route)
            page.route("**/api/empleados", pending_list)
            with page.expect_request(lambda request: urlparse(request.url).path == "/api/empleados"):
                page.reload(wait_until="domcontentloaded")
            expect(page.get_by_role("status").filter(has_text="Cargando empleados")).to_be_visible()
            assert len(held_requests) == 1
            held_requests[0].fulfill(status=503, content_type="application/problem+json", body='{"title":"Servicio temporalmente no disponible"}')
            expect(page.get_by_role("alert")).to_contain_text("Servicio temporalmente no disponible")
            page.unroute("**/api/empleados", pending_list)
            page.route("**/api/empleados", lambda route: route.fulfill(status=200, content_type="application/json", body="[]"))
            page.get_by_role("button", name="Reintentar", exact=True).click()
            expect(page.get_by_text("No hay empleados registrados.", exact=True)).to_be_visible()
            page.unroute("**/api/empleados")

            stage = "logout clears the entire app session"
            page.get_by_role("button", name="Cerrar sesión", exact=True).click()
            expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            session_cleared(page)

            stage = "seller hidden menu and real API 403 on direct URL"
            login(page, data["seller"])
            expect(page.get_by_role("link", name="Empleados", exact=True)).to_have_count(0)
            # A tampered cached role must not turn the seller into an administrator.
            page.evaluate("sessionStorage.setItem('importnet.roles', JSON.stringify(['Administrador']))")
            with page.expect_response(response_is("GET", "/api/empleados")) as pending:
                page.goto(url + "/empleados")
            assert pending.value.status == 403
            expect(page.get_by_role("heading", name="Acceso denegado", exact=True)).to_be_visible()
            expect(page.get_by_role("button", name="Nuevo empleado", exact=True)).to_have_count(0)
            expect(page.get_by_role("link", name="Empleados", exact=True)).to_have_count(0)
            assert page.evaluate("sessionStorage.getItem('importnet.token')") is not None

            stage = "a separate tab has no session"
            other_tab = page.context.new_page()
            other_tab.goto(url + "/empleados")
            expect(other_tab).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            session_cleared(other_tab)
            other_tab.close()

            stage = "expired token guard clears session"
            page.evaluate("sessionStorage.setItem('importnet.expiration', '2000-01-01T00:00:00Z')")
            page.goto(url + "/empleados")
            expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            session_cleared(page)

            stage = "invalid token receives real 401 and redirects"
            login(page, data["admin"])
            page.evaluate("sessionStorage.setItem('importnet.token', 'invalid-token')")
            with page.expect_response(response_is("GET", "/api/auth/me")) as pending:
                page.goto(url + "/empleados")
            assert pending.value.status == 401
            expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            session_cleared(page)
            print("Astro employees: admin CRUD, validation, loading/error/empty, seller API 403, isolated tab, logout, expired token and API 401 passed")
        finally:
            browser.close()
except Exception as error:
    diagnostic = (str(error).splitlines() or ["Assertion failed"])[0]
    for account in (data["admin"], data["seller"]):
        diagnostic = diagnostic.replace(account["password"], "[redacted]").replace(account["email"], "[redacted]")
    print("Frontend verification failed at " + stage + ": " + type(error).__name__ + " " + diagnostic, file=sys.stderr)
    sys.exit(1)
