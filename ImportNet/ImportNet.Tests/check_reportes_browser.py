"""Verify a real authenticated download through the Astro proxy."""
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
from urllib.parse import urlparse
from xml.etree import ElementTree
from zipfile import ZipFile

from playwright.sync_api import expect, sync_playwright

datos = json.load(sys.stdin)
frontend = Path(__file__).resolve().parent.parent / "frontend"
astro = str(frontend / "node_modules/astro/bin/astro.mjs")
assert urlparse(datos["url"]).hostname in ("localhost", "127.0.0.1")
with socket.socket() as sock:
    sock.bind(("127.0.0.1", 0))
    puerto = sock.getsockname()[1]

url = f"http://127.0.0.1:{puerto}"
entorno = dict(os.environ, API_URL=datos["url"])
etapa = "arranque de Astro"
iniciado = False
try:
    resultado = subprocess.run(
        ["node", astro, "dev", "--background", "--host", "127.0.0.1", "--port", str(puerto)],
        cwd=frontend, env=entorno, capture_output=True, timeout=45,
    )
    iniciado = resultado.returncode == 0
    assert iniciado, "Astro no arrancó. Ejecuta npm ci y comprueba la versión de Node."
    with sync_playwright() as pw:
        browser = pw.chromium.launch(channel="chrome")
        try:
            pagina = browser.new_page(accept_downloads=True)
            etapa = "login"
            pagina.goto(url + "/login")
            pagina.get_by_label("Correo", exact=True).fill(datos["email"])
            pagina.get_by_label("Contraseña", exact=True).fill(datos["password"])
            pagina.get_by_role("button", name="Ingresar", exact=True).click()
            pagina.get_by_role("link", name="Reportes", exact=True).click()

            etapa = "tabla y filtros"
            pagina.get_by_label("Desde", exact=True).fill("2026-11-20")
            pagina.get_by_label("Hasta", exact=True).fill("2026-11-20")
            pagina.get_by_label("Agrupar por", exact=True).select_option("Mes")
            pagina.get_by_role("button", name="Aplicar filtros", exact=True).click()
            expect(pagina.get_by_text("2026-11-20 al 2026-11-20 · Agrupación por mes")).to_be_visible()
            total = pagina.get_by_role("row").filter(has_text="Totales")
            expect(total).to_contain_text("125.50")

            etapa = "descarga con los filtros aplicados"
            # Un cambio sin aplicar no debe cambiar el periodo descargado.
            pagina.get_by_label("Desde", exact=True).fill("2026-11-01")
            with pagina.expect_download() as pendiente:
                pagina.get_by_role("button", name="Descargar Excel", exact=True).click()
            descarga = pendiente.value
            assert descarga.suggested_filename == "ventas-por-periodo-2026-11-20-al-2026-11-20-mes.xlsx"
            with ZipFile(descarga.path()) as libro:
                hoja = ElementTree.fromstring(libro.read("xl/worksheets/sheet1.xml"))
                assert hoja.find(".//{*}c[@r='C5']/{*}v").text == "125.5"

            etapa = "inventario sin filtro de periodo"
            pagina.get_by_label("Tipo de reporte").select_option("stock-bajo")
            expect(pagina.get_by_role("heading", name="Stock bajo")).to_be_visible()
            expect(pagina.get_by_label("Desde", exact=True)).to_have_count(0)
            print("Astro Reportes: tabla filtrada y archivo Excel coinciden en S/ 125.50.")
        finally:
            browser.close()
except Exception as error:
    print(f"Reportes browser: {etapa} ({type(error).__name__})", file=sys.stderr)
    sys.exit(1)
finally:
    if iniciado:
        subprocess.run(["node", astro, "dev", "stop"], cwd=frontend, env=entorno, capture_output=True, timeout=20)
