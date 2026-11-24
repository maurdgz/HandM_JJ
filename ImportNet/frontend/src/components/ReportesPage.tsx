import { useEffect, useState, type SubmitEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { api, apiExcel } from "@/lib/api";
import { fechaPeru, periodoMeses } from "@/lib/dashboard";

const reportes = [
  { id: "ventas-por-periodo", nombre: "Ventas por periodo" },
  { id: "ventas-por-vendedor", nombre: "Ventas por vendedor" },
  { id: "top-productos", nombre: "Productos más vendidos" },
  { id: "margen-productos", nombre: "Margen por producto" },
  { id: "stock-bajo", nombre: "Stock bajo" },
  { id: "cierre-caja", nombre: "Cierre de caja" },
  { id: "compras-por-proveedor", nombre: "Compras por proveedor" },
] as const;

type Reporte = typeof reportes[number]["id"];
type Celda = string | number | null;
type Formato = "texto" | "entero" | "cantidad" | "pen" | "pen4" | "usd" | "porcentaje";

interface Tabla {
  titulo: string;
  filtros: string;
  nombreArchivo: string;
  columnas: { titulo: string; formato: Formato }[];
  filas: Celda[][];
  totales: Celda[];
}

const formatos = {
  entero: new Intl.NumberFormat("es-PE", { maximumFractionDigits: 0 }),
  cantidad: new Intl.NumberFormat("es-PE", { maximumFractionDigits: 3 }),
  pen: new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }),
  pen4: new Intl.NumberFormat("es-PE", {
    style: "currency", currency: "PEN", minimumFractionDigits: 4, maximumFractionDigits: 4,
  }),
  usd: new Intl.NumberFormat("es-PE", { style: "currency", currency: "USD", currencyDisplay: "code" }),
  porcentaje: new Intl.NumberFormat("es-PE", { style: "percent", minimumFractionDigits: 2, maximumFractionDigits: 2 }),
};

function mostrar(celda: Celda, formato: Formato) {
  if (celda === null) return "—";
  if (typeof celda !== "number" || formato === "texto") return celda;
  return formatos[formato].format(formato === "porcentaje" ? celda / 100 : celda);
}

function Fila({ celdas, tabla, total = false }: { celdas: Celda[]; tabla: Tabla; total?: boolean }) {
  return <TableRow>
    {celdas.map((celda, i) => {
      const formato = tabla.columnas[i].formato;
      if (i === 0 && total) return <th key={i} scope="row" className="p-2 text-left">{celda}</th>;
      return <TableCell key={i} className={formato === "texto" ? undefined : "whitespace-nowrap text-right tabular-nums"}>
        {mostrar(celda, formato)}
      </TableCell>;
    })}
  </TableRow>;
}

function TablaVisual({ tabla }: { tabla: Tabla }) {
  return <section aria-labelledby="titulo-reporte" className="space-y-3">
    <div>
      <h2 id="titulo-reporte" className="text-lg font-semibold">{tabla.titulo}</h2>
      <p className="text-sm text-slate-600">{tabla.filtros}</p>
    </div>
    <div className="rounded-lg border">
      <Table aria-labelledby="titulo-reporte">
        <TableHeader>
          <TableRow>
            {tabla.columnas.map((columna) => <TableHead key={columna.titulo} scope="col"
              className={columna.formato === "texto" ? undefined : "whitespace-nowrap text-right"}>
              {columna.titulo}
            </TableHead>)}
          </TableRow>
        </TableHeader>
        <TableBody>
          {tabla.filas.length === 0 && <TableRow><TableCell colSpan={tabla.columnas.length}>
            No hay datos para estos filtros.
          </TableCell></TableRow>}
          {tabla.filas.map((fila, i) => <Fila key={i} celdas={fila} tabla={tabla} />)}
        </TableBody>
        <TableFooter><Fila celdas={tabla.totales} tabla={tabla} total /></TableFooter>
      </Table>
    </div>
  </section>;
}

function VistaReporte({ reporte }: { reporte: Reporte }) {
  const conRango = reporte !== "stock-bajo" && reporte !== "cierre-caja";
  const rango = periodoMeses(1);
  const [consulta, setConsulta] = useState(() => ({
    parametros: new URLSearchParams(
      conRango ? { ...rango } : reporte === "cierre-caja" ? { fecha: fechaPeru() } : {},
    ).toString(),
  }));
  const [tabla, setTabla] = useState<Tabla | null>(null);
  const [error, setError] = useState("");
  const [errorExcel, setErrorExcel] = useState("");
  const [exportando, setExportando] = useState(false);

  useEffect(() => {
    const controlador = new AbortController();
    api<Tabla>(`/reportes/${reporte}/tabla?${consulta.parametros}`, { signal: controlador.signal })
      .then((datos) => { if (!controlador.signal.aborted) setTabla(datos); })
      .catch((e: unknown) => {
        if (!controlador.signal.aborted) setError(e instanceof Error ? e.message : "No se pudo cargar el reporte.");
      });
    return () => controlador.abort();
  }, [reporte, consulta]);

  function consultar(evento: SubmitEvent<HTMLFormElement>) {
    evento.preventDefault();
    const parametros = new URLSearchParams();
    for (const [clave, valor] of new FormData(evento.currentTarget)) {
      if (typeof valor === "string" && valor) parametros.set(clave, valor);
    }
    setTabla(null);
    setErrorExcel("");
    setError("");
    setConsulta({ parametros: parametros.toString() });
  }

  async function exportar() {
    if (!tabla) return;
    setExportando(true);
    setErrorExcel("");
    try {
      const archivo = await apiExcel(`/reportes/${reporte}/excel?${consulta.parametros}`);
      const url = URL.createObjectURL(archivo);
      const enlace = document.createElement("a");
      enlace.href = url;
      enlace.download = tabla.nombreArchivo + ".xlsx";
      enlace.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (e) {
      setErrorExcel(e instanceof Error ? e.message : "No se pudo descargar el Excel.");
    } finally {
      setExportando(false);
    }
  }

  return <div className="space-y-4">
    <form onSubmit={consultar} className="flex flex-wrap items-end gap-3">
      {conRango && <>
        <div className="space-y-2"><Label htmlFor="desde-reporte">Desde</Label>
          <Input id="desde-reporte" type="date" name="desde" defaultValue={rango.desde} required /></div>
        <div className="space-y-2"><Label htmlFor="hasta-reporte">Hasta</Label>
          <Input id="hasta-reporte" type="date" name="hasta" defaultValue={rango.hasta} required /></div>
      </>}
      {reporte === "cierre-caja" && <div className="space-y-2"><Label htmlFor="fecha-reporte">Fecha</Label>
        <Input id="fecha-reporte" type="date" name="fecha" defaultValue={fechaPeru()} required /></div>}
      {reporte === "ventas-por-periodo" && <div className="space-y-2">
        <Label htmlFor="agrupacion-reporte">Agrupar por</Label>
        <select id="agrupacion-reporte" name="agrupacion" className="block h-9 rounded-md border bg-background px-3 text-sm">
          <option value="Dia">Día</option>
          <option value="Mes">Mes</option>
        </select>
      </div>}
      {reporte === "top-productos" && <div className="space-y-2"><Label htmlFor="top-reporte">Máximo de productos</Label>
        <Input id="top-reporte" className="w-24" type="number" name="top" min={1} max={50} defaultValue={10} required /></div>}
      <Button type="submit" disabled={exportando}>{reporte === "stock-bajo" ? "Actualizar" : "Aplicar filtros"}</Button>
      <Button type="button" variant="outline" disabled={!tabla || exportando} onClick={() => void exportar()}>
        {exportando ? "Exportando…" : "Descargar Excel"}
      </Button>
    </form>
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    {errorExcel && <p role="alert" className="text-sm text-red-700">{errorExcel}</p>}
    {!tabla && !error && <p role="status">Cargando reporte…</p>}
    {tabla && <TablaVisual tabla={tabla} />}
  </div>;
}

export default function ReportesPage() {
  const [reporte, setReporte] = useState<Reporte>("ventas-por-periodo");

  return <section className="space-y-5">
    <h1 className="text-2xl font-semibold">Reportes</h1>
    <div className="space-y-2">
      <Label htmlFor="reporte">Tipo de reporte</Label>
      <select id="reporte" value={reporte} onChange={(evento) => {
        const seleccionado = reportes.find((item) => item.id === evento.target.value);
        if (seleccionado) setReporte(seleccionado.id);
      }} className="block h-9 max-w-full rounded-md border bg-background px-3 text-sm">
        {reportes.map((item) => <option key={item.id} value={item.id}>{item.nombre}</option>)}
      </select>
    </div>
    <VistaReporte key={reporte} reporte={reporte} />
  </section>;
}
