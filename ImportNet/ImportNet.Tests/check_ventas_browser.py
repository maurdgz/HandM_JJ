"""Comprueba el build Astro servido por la API usando datos de prueba aislados."""
import json
import re
import sys
from datetime import datetime, timedelta, timezone
from playwright.sync_api import sync_playwright, expect

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")
stage = "inicio"
data = json.load(sys.stdin)
url = data["url"].rstrip("/")


def login(page, user):
    page.goto(url + "/login", wait_until="domcontentloaded")
    page.get_by_label("Correo", exact=True).fill(user["email"])
    page.get_by_label("Contraseña", exact=True).fill(user["password"])
    page.get_by_role("button", name="Ingresar", exact=True).click()
    expect(page.get_by_role("navigation", name="Principal")).to_be_visible()


def api_status(page, path, method="GET", body=None):
    return page.evaluate("""async ({path,method,body}) => {
        const response = await fetch(path, {method, headers: {
            Authorization:'Bearer '+sessionStorage.getItem('importnet.token'),
            'Content-Type':'application/json'
        }, ...(body === null ? {} : {body:JSON.stringify(body)})});
        return response.status;
    }""", {"path": path, "method": method, "body": body})


try:
    with sync_playwright() as pw:
        browser = pw.chromium.launch(channel="chrome", headless=True)
        try:
            page = browser.new_page()
            page.set_default_timeout(30000)
            expect.set_options(timeout=30000)
            stage = "redireccion sin sesion"
            page.goto(url + "/ventas/historial", wait_until="domcontentloaded")
            expect(page).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
            stage = "login y filtro de historial"
            login(page, data["admin"])
            page.get_by_role("link", name="Historial de ventas", exact=True).click()
            own_row = page.get_by_role("row").filter(has=page.get_by_role("cell", name=str(data["ownNumber"]), exact=True))
            expect(own_row).to_be_visible()
            today = datetime.now(timezone(timedelta(hours=-5))).date()
            tomorrow = (today + timedelta(days=1)).isoformat()
            page.get_by_label("Hasta", exact=True).fill(tomorrow)
            page.get_by_label("Desde", exact=True).fill(tomorrow)
            page.get_by_role("button", name="Filtrar", exact=True).click()
            expect(page.get_by_role("status")).to_have_text("No hay ventas en el periodo seleccionado.")
            page.get_by_label("Desde", exact=True).fill(today.isoformat())
            page.get_by_label("Hasta", exact=True).fill(today.isoformat())
            page.get_by_role("button", name="Filtrar", exact=True).click()
            expect(own_row).to_be_visible()
            stage = "nota y estilos de impresion"
            own_row.get_by_role("link", name="Ver nota", exact=True).click()
            article = page.locator("article")
            expect(article).to_contain_text(str(data["ownNumber"]).zfill(8))
            for text in ("Vendedor:", "Prueba API", "Cliente:", "Olla de prueba", "Organizador de prueba", "Total"):
                expect(article).to_contain_text(text)
            expect(article.locator("dd").last).to_contain_text("27.00")
            expect(article).to_contain_text(re.compile(rf"0?{today.day}/0?{today.month}/(?:20)?{today.year % 100:02d}"))
            page.emulate_media(media="print")
            expect(page.locator("aside")).to_be_hidden()
            expect(page.get_by_role("button", name="Imprimir nota", exact=True)).to_be_hidden()
            expect(article).to_be_visible()
            page.emulate_media(media="screen")
            stage = "anulacion y rechazo del segundo intento"
            page.get_by_role("link", name="Volver al historial", exact=True).click()
            own_row.get_by_role("button", name="Anular", exact=True).click()
            page.get_by_label("Motivo de anulación", exact=True).fill("Devolucion de prueba de navegador")
            with page.expect_response(lambda r: r.request.method == "POST" and f"/api/ventas/{data['ownId']}/anulacion" in r.url) as pending:
                page.get_by_role("button", name="Confirmar anulación", exact=True).click()
            assert pending.value.status == 204
            expect(own_row).to_contain_text("Anulada")
            expect(own_row.get_by_role("button", name="Anular", exact=True)).to_have_count(0)
            assert api_status(page, f"/api/ventas/{data['ownId']}/anulacion", "POST", {"motivo": "Segundo intento"}) == 409
            own_row.get_by_role("link", name="Ver nota", exact=True).click()
            expect(page.locator("article")).to_contain_text("VENTA ANULADA")
            stage = "vendedor y venta ajena"
            seller_context = browser.new_context()
            try:
                seller = seller_context.new_page()
                login(seller, data["seller"])
                expect(seller.get_by_role("link", name="Empleados", exact=True)).to_have_count(0)
                seller.goto(url + "/ventas/historial", wait_until="domcontentloaded")
                expect(seller.get_by_role("table").get_by_role("row")).to_have_count(2)
                expect(seller.get_by_role("button", name="Anular", exact=True)).to_have_count(0)
                assert api_status(seller, f"/api/ventas/{data['foreignId']}") == 404
                assert api_status(seller, f"/api/ventas/{data['ownId']}/anulacion", "POST", {"motivo": "Sin permiso"}) == 403
                seller.goto(url + f"/ventas/nota?id={data['foreignId']}", wait_until="domcontentloaded")
                expect(seller.get_by_role("alert")).to_be_visible()
                expect(seller.locator("article")).to_have_count(0)
                stage = "token invalido cierra sesion"
                seller.evaluate("sessionStorage.setItem('importnet.token','invalid-token')")
                seller.goto(url + "/ventas/historial", wait_until="domcontentloaded")
                expect(seller).to_have_url(re.compile(re.escape(url) + r"/login/?$"))
                assert seller.evaluate("sessionStorage.getItem('importnet.token')") is None
            finally:
                seller_context.close()
            print("Chrome: host unico, login, filtros, nota, impresion, anulacion, roles y cierre por 401 correctos.")
        finally:
            browser.close()
except Exception as error:
    print(f"Browser verification failed at {stage}: {type(error).__name__}", file=sys.stderr)
    sys.exit(1)
