"""Real Astro + API browser check; isolated fixtures, credentials only on stdin."""
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import time
import urllib.request
from playwright.sync_api import sync_playwright, expect

stage = "startup"
responses = []
sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")
data = json.load(sys.stdin)
frontend = Path(__file__).resolve().parent.parent / "frontend"
astro = str(frontend / "node_modules/astro/bin/astro.mjs")
with socket.socket() as sock:
    sock.bind(("127.0.0.1", 0))
    port = sock.getsockname()[1]
url = f"http://127.0.0.1:{port}"
env = dict(os.environ, API_URL=data["apiUrl"].rstrip("/"))
started = False
try:
    result = subprocess.run(["node", astro, "dev", "--background", "--host", "127.0.0.1", "--port", str(port)], cwd=frontend, env=env, capture_output=True, timeout=45)
    started = result.returncode == 0
    assert started
    with sync_playwright() as pw:
        browser = pw.chromium.launch(channel="chrome", headless=True)
        try:
            page = browser.new_page()
            page.on("response", lambda r: responses.append((r.url.split("/api/")[-1].split("?")[0], r.status)) if "/api/" in r.url else None)
            page.set_default_timeout(45000)
            expect.set_options(timeout=30000)
            stage = "anonymous redirect"
            page.goto(url + "/compras", wait_until="domcontentloaded")
            expect(page).to_have_url(url + "/login")
            def login(page, user):
                global stage
                stage = "login page"
                page.goto(url + "/login", wait_until="domcontentloaded")
                stage = "login email field"
                page.get_by_label("Correo", exact=True).fill(user["email"])
                stage = "login password field"
                page.get_by_label("Contraseña", exact=True).fill(user["password"])
                stage = "login submit"
                page.get_by_role("button", name="Ingresar", exact=True).click()
                stage = "login navigation"
                expect(page.get_by_role("navigation", name="Principal")).to_be_visible()
            stage = "admin login and navigation"
            login(page, data["admin"])
            page.get_by_role("link", name="Compras de importación", exact=True).click()
            page.get_by_role("button", name="Nueva compra", exact=True).click()
            stage = "create draft form"
            stage = "supplier selector"
            page.get_by_label("Proveedor", exact=True).select_option(str(data["proveedorId"]))
            page.get_by_label("Tipo de cambio manual (PEN por USD)", exact=True).fill("3.8")
            page.get_by_label("Flete PEN", exact=True).fill("10")
            page.get_by_label("Aduana / agente PEN", exact=True).fill("5")
            stage = "product selector"
            page.get_by_label("Producto línea 1", exact=True).select_option(str(data["productoId"]))
            page.get_by_label("Cantidad línea 1", exact=True).fill("2")
            page.get_by_label("Costo USD línea 1", exact=True).fill("10")
            expect(page.get_by_role("complementary", name="Vista previa de costos")).to_contain_text("91")
            with page.expect_response(lambda r: r.request.method == "POST" and r.url.endswith("/api/compras")) as pending:
                page.get_by_role("button", name="Guardar borrador", exact=True).click()
            assert pending.value.status == 201, f"Create returned {pending.value.status}"
            created = pending.value.json()
            assert created["estado"] == "Borrador" and created["totalPen"] == 91, "Unexpected draft totals"
            identifier = created["id"]
            expect(page.get_by_text(f"Borrador #{identifier} guardado.", exact=False)).to_be_visible()
            stage = "edit draft and confirmed total"
            page.get_by_role("button", name=f"Editar #{identifier}", exact=True).click()
            page.get_by_label("Flete PEN", exact=True).fill("20")
            with page.expect_response(lambda r: r.request.method == "PUT" and r.url.endswith(f"/api/compras/{identifier}")) as pending:
                page.get_by_role("button", name="Guardar borrador", exact=True).click()
            assert pending.value.status == 200 and pending.value.json()["totalPen"] == 101, f"Edit returned {pending.value.status}: {pending.value.json().get('title', 'Unexpected total')}"
            stage = "received readonly"
            page.get_by_role("button", name=f"Consultar #{data['recibidaId']}", exact=True).click()
            expect(page.get_by_text("Compra recibida: solo consulta.", exact=False)).to_be_visible()
            expect(page.get_by_role("button", name=f"Editar #{data['recibidaId']}", exact=True)).to_have_count(0)
            expect(page.get_by_role("button", name=f"Eliminar #{data['recibidaId']}", exact=True)).to_have_count(0)
            stage = "delete draft"
            page.on("dialog", lambda d: d.accept())
            with page.expect_response(lambda r: r.request.method == "DELETE" and r.url.endswith(f"/api/compras/{identifier}")) as pending:
                page.get_by_role("button", name=f"Eliminar #{identifier}", exact=True).click()
            assert pending.value.status == 204, f"Delete returned {pending.value.status}"
            stage = "seller forced URL and API 403"
            seller = browser.new_page()
            login(seller, data["seller"])
            expect(seller.get_by_role("link", name="Compras de importación", exact=True)).to_have_count(0)
            seller.goto(url + "/compras", wait_until="domcontentloaded")
            expect(seller.get_by_role("alert")).to_contain_text("Acceso restringido")
            status = seller.evaluate("async () => (await fetch('/api/compras', {headers:{Authorization:'Bearer '+sessionStorage.getItem('importnet.token')}})).status")
            assert status == 403
            stage = "API 401 clears session"
            page.route("**/api/compras*", lambda route: route.fulfill(status=401, content_type="application/problem+json", body='{"title":"Unauthorized"}'))
            page.goto(url + "/compras", wait_until="domcontentloaded")
            expect(page).to_have_url(url + "/login")
            assert page.evaluate("sessionStorage.getItem('importnet.token')") is None
            print("Astro Chrome: admin CRUD, confirmed PEN, received readonly, seller 403 and session 401 passed")
        finally:
            browser.close()
except Exception as error:
    diagnostic = (str(error).splitlines() or ["Assertion failed"])[0]
    for account in (data["admin"], data["seller"]):
        diagnostic = diagnostic.replace(account["password"], "[redacted]").replace(account["email"], "[redacted]")
    print("Browser verification failed at " + stage + ": " + type(error).__name__ + " " + diagnostic + " API statuses: " + json.dumps(responses), file=sys.stderr)
    sys.exit(1)
finally:
    if started:
        subprocess.run(["node", astro, "dev", "stop"], cwd=frontend, env=env, capture_output=True, timeout=20)
