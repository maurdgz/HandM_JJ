"""Astro + API + PostgreSQL in an isolated test schema. Credentials only via stdin."""
import json
import os
import re
from pathlib import Path
import socket
import subprocess
import sys
from playwright.sync_api import sync_playwright, expect

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")
data = json.load(sys.stdin)
frontend = Path(__file__).resolve().parent.parent / "frontend"
astro = str(frontend / "node_modules/astro/bin/astro.mjs")
evidence = Path(__file__).resolve().parent / "obj"
evidence.mkdir(exist_ok=True)
with socket.socket() as sock:
    sock.bind(("127.0.0.1", 0))
    port = sock.getsockname()[1]
url = data["apiUrl"].rstrip("/") if data["singleHost"] else f"http://127.0.0.1:{port}"
env = dict(os.environ, API_URL=data["apiUrl"].rstrip("/"))
stage = "startup"
started = False


def login(page, user):
    page.goto(url + "/login", wait_until="domcontentloaded")
    page.get_by_label("Correo", exact=True).fill(user["email"])
    page.get_by_label("Contraseña", exact=True).fill(user["password"])
    page.get_by_role("button", name="Ingresar", exact=True).click()
    expect(page.get_by_role("navigation", name="Principal")).to_be_visible()


def request(page, method, path, body=None):
    return page.evaluate("""async ({method,path,body}) => {
      const r = await fetch('/api'+path, {method,
        headers: {Authorization:'Bearer '+sessionStorage.getItem('importnet.token'), 'Content-Type':'application/json'},
        body: body == null ? undefined : JSON.stringify(body)});
      return {status:r.status, data:await r.json()};
    }""", {"method": method, "path": path, "body": body})


def venta_ui(page, producto, cliente=False):
    page.goto(url + "/ventas", wait_until="domcontentloaded")
    page.get_by_role("button", name=producto, exact=False).click()
    if cliente:
        page.get_by_label("DNI o RUC (opcional)", exact=True).fill("90909090")
        expect(page.get_by_role("button", name="Registrar venta", exact=True)).to_be_disabled()
        page.get_by_role("button", name="Buscar cliente", exact=True).click()
        expect(page.get_by_role("status").filter(has_text="Cliente navegador")).to_be_visible()
        page.get_by_label("Descuento Producto POS", exact=True).fill("5")
    with page.expect_response(lambda r: r.url.endswith("/api/ventas") and r.request.method == "POST") as pending:
        page.get_by_role("button", name="Registrar venta", exact=True).click()
    assert pending.value.status == 201
    venta = pending.value.json()
    expect(page.get_by_role("status").filter(has_text="registrada")).to_be_visible()
    return venta


try:
    if not data["singleHost"]:
        result = subprocess.run(["node", astro, "dev", "--background", "--host", "127.0.0.1", "--port", str(port)],
                                cwd=frontend, env=env, capture_output=True, timeout=45)
        started = result.returncode == 0
        assert started
    with sync_playwright() as pw:
        browser = pw.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 1000})
        errors = []
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.set_default_timeout(30000)
        expect.set_options(timeout=30000)
        stage = "anonymous redirect"
        page.goto(url, wait_until="domcontentloaded")
        expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
        stage = "admin login and initial data"
        login(page, data["admin"])
        expect(page.get_by_role("heading", name="Dashboard", exact=True)).to_be_visible()
        expect(page.locator('[data-kpi="Ventas de hoy"]')).to_contain_text("0.00")
        expect(page.locator('[data-slot="chart"]')).to_have_count(3)
        stage = "period selector changes all chart queries"
        with page.expect_response(lambda r: "/reportes/top-productos?" in r.url and r.request.method == "GET") as pending:
            page.get_by_role("combobox", name="Periodo de los gráficos").click()
            page.get_by_role("option", name="Este mes", exact=True).click()
        assert pending.value.json() == []
        expect(page.get_by_text("No hay productos vendidos en este periodo.", exact=True)).to_be_visible()
        stage = "POS selects client and commits real sale"
        venta = venta_ui(page, "POS-A", cliente=True)
        assert venta["total"] == 95
        stage = "dashboard agrees with sale and margin"
        page.goto(url, wait_until="domcontentloaded")
        expect(page.locator('[data-kpi="Ventas de hoy"]')).to_contain_text("95.00")
        expect(page.locator('[data-kpi="Margen del mes"]')).to_contain_text("75.00")
        expect(page.locator('[data-slot="chart"]')).to_have_count(3)
        page.get_by_text("Ver datos por mes", exact=True).click()
        expect(page.get_by_role("table").first).to_contain_text("95.00")
        page.screenshot(path=str(evidence / "dashboard-desktop.png"), full_page=True)
        stage = "mobile layout"
        page.set_viewport_size({"width": 390, "height": 844})
        expect(page.locator('[data-kpi="Ventas de hoy"]')).to_be_visible()
        assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth")
        page.screenshot(path=str(evidence / "dashboard-mobile.png"), full_page=True)
        page.set_viewport_size({"width": 1440, "height": 1000})
        stage = "single month remains visible and excludes previous months"
        with page.expect_response(lambda r: "/reportes/ventas-por-periodo?" in r.url) as pending:
            page.get_by_role("combobox", name="Periodo de los gráficos").click()
            page.get_by_role("option", name="Este mes", exact=True).click()
        assert len(pending.value.json()) == 1 and pending.value.json()[0]["total"] == 95
        expect(page.locator("circle.recharts-dot")).to_have_count(1)
        stage = "annulled sale excluded and stock restored"
        respuesta = request(page, "POST", f"/ventas/{venta['id']}/anulacion", {"motivo": "Verificación de dashboard"})
        assert respuesta["status"] == 200
        page.get_by_role("button", name="Actualizar", exact=True).click()
        expect(page.locator('[data-kpi="Ventas de hoy"]')).to_contain_text("0.00")
        stage = "error and retry states"
        page.route("**/api/reportes/dashboard", lambda route: route.fulfill(status=500, content_type="application/problem+json", body='{"title":"Error de verificación"}'))
        page.get_by_role("button", name="Actualizar", exact=True).click()
        expect(page.get_by_role("alert")).to_contain_text("Error de verificación")
        page.unroute("**/api/reportes/dashboard")
        page.get_by_role("button", name="Reintentar", exact=True).click()
        expect(page.locator('[data-kpi="Ventas de hoy"]')).to_contain_text("0.00")
        stage = "seller own daily totals and restricted requests"
        seller = browser.new_page(viewport={"width": 390, "height": 844})
        seller_requests = []
        seller.on("request", lambda r: seller_requests.append(r.url.split("/api/")[-1]) if "/api/reportes/" in r.url else None)
        login(seller, data["seller"])
        expect(seller.get_by_role("heading", name="Mis ventas de hoy", exact=True)).to_be_visible()
        expect(seller.get_by_text("Todavía no has registrado ventas hoy.", exact=True)).to_be_visible()
        seller_sale = venta_ui(seller, "POS-B")
        assert seller_sale["total"] == 50
        seller.goto(url, wait_until="domcontentloaded")
        expect(seller.get_by_role("main")).to_contain_text("50.00")
        assert all(p.startswith("reportes/mi-resumen") for p in seller_requests)
        assert request(seller, "GET", "/reportes/dashboard")["status"] == 403
        stage = "insufficient stock rejected after sale"
        insuficiente = request(seller, "POST", "/ventas", {"lineas": [{"productoId": data["segundo"], "cantidad": 1, "descuento": 0}]})
        assert insuficiente["status"] == 409
        stage = "Swagger documents bearer and new endpoint"
        swagger = page.request.get(data["apiUrl"].rstrip("/") + "/swagger/v1/swagger.json").json()
        assert "/api/reportes/dashboard" in swagger["paths"]
        assert swagger["components"]["securitySchemes"]["Bearer"]["scheme"] == "bearer"
        assert not errors
        modo = "un solo host" if data["singleHost"] else "Astro dev + proxy"
        print(f"Dashboard Chrome + PostgreSQL ({modo}): KPI, gráficos, periodo, móvil, cliente, POS, anulación, roles, error/reintento y Swagger correctos.")
        browser.close()
except Exception:
    print(f"Dashboard browser failed at {stage}", file=sys.stderr)
    raise
finally:
    if started:
        subprocess.run(["node", astro, "dev", "stop"], cwd=frontend, env=env, capture_output=True, timeout=30)
