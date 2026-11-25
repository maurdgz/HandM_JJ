import { useCallback, useEffect, useState, type SubmitEvent } from "react";
import { toast } from "sonner";

import { api } from "@/lib/api";
import type { Pagina } from "@/lib/compras";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface ProductoStock {
  productoId: number;
  codigo: string;
  nombre: string;
  unidadMedida: string;
  cantidad: number;
}

interface LineaKardex {
  movimientoId: number;
  fecha: string;
  tipo: "Entrada" | "Salida" | "Ajuste";
  referencia: string;
  entrada: number;
  salida: number;
  saldo: number;
  empleado: string;
  motivo: string | null;
  observacion: string | null;
}

interface Kardex {
  productoId: number;
  codigo: string;
  nombre: string;
  unidadMedida: string;
  saldoInicial: number;
  totalEntradas: number;
  totalSalidas: number;
  saldoFinal: number;
  stockActual: number;
  movimientos: LineaKardex[];
}

interface AjusteRegistrado {
  diferencia: number;
  stockNuevo: number;
}

type Motivo = "Merma" | "Rotura" | "ConteoFisico" | "DevolucionProveedor";

const motivos: { valor: Motivo; texto: string }[] = [
  { valor: "Merma", texto: "Merma" },
  { valor: "Rotura", texto: "Rotura" },
  { valor: "ConteoFisico", texto: "Conteo físico" },
  { valor: "DevolucionProveedor", texto: "Devolución a proveedor" },
];

const cantidad = new Intl.NumberFormat("es-PE", { maximumFractionDigits: 3 });
const fechaHora = new Intl.DateTimeFormat("es-PE", {
  dateStyle: "short",
  timeStyle: "short",
  timeZone: "America/Lima",
});

function productoEnUrl(): number | null {
  const valor = Number(new URLSearchParams(window.location.search).get("producto"));
  return Number.isInteger(valor) && valor > 0 ? valor : null;
}

export default function KardexPage() {
  const [texto, setTexto] = useState("");
  const [resultados, setResultados] = useState<ProductoStock[] | null>(null);
  const [buscando, setBuscando] = useState(false);

  const [productoId, setProductoId] = useState<number | null>(productoEnUrl);
  const [desde, setDesde] = useState("");
  const [hasta, setHasta] = useState("");
  const [filtros, setFiltros] = useState({ desde: "", hasta: "" });

  const [kardex, setKardex] = useState<Kardex | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState("");
  const [recarga, setRecarga] = useState(0);

  const [ajusteAbierto, setAjusteAbierto] = useState(false);

  useEffect(() => {
    if (productoId === null) return;

    const controller = new AbortController();
    const parametros = new URLSearchParams();
    if (filtros.desde) parametros.set("desde", filtros.desde);
    if (filtros.hasta) parametros.set("hasta", filtros.hasta);

    setCargando(true);
    setError("");

    api<Kardex>(`/inventario/kardex/${productoId}?${parametros.toString()}`, {
      signal: controller.signal,
    })
      .then(setKardex)
      .catch((e: unknown) => {
        if (controller.signal.aborted) return;
        setKardex(null);
        setError(e instanceof Error ? e.message : "No se pudo consultar el kardex.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setCargando(false);
      });

    return () => controller.abort();
  }, [productoId, filtros, recarga]);

  async function buscar(event: SubmitEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setBuscando(true);

    try {
      const parametros = new URLSearchParams({ texto: texto.trim(), tamano: "10" });
      const pagina = await api<Pagina<ProductoStock>>(`/inventario/stock?${parametros.toString()}`);
      setResultados(pagina.items);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "No se pudo buscar productos.");
    } finally {
      setBuscando(false);
    }
  }

  function seleccionar(id: number): void {
    setProductoId(id);
    setResultados(null);
    setKardex(null);
    window.history.replaceState(null, "", `/kardex?producto=${id}`);
  }

  function filtrar(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (desde && hasta && desde > hasta) {
      toast.error("La fecha inicial no puede ser posterior a la final.");
      return;
    }

    setFiltros({ desde, hasta });
  }

  function limpiarFiltros(): void {
    setDesde("");
    setHasta("");
    setFiltros({ desde: "", hasta: "" });
  }

  const recargar = useCallback(() => setRecarga((n) => n + 1), []);

  // El saldo solo se compara con el stock cuando el kardex llega hasta hoy.
  const cuadra = kardex && !filtros.hasta ? kardex.saldoFinal === kardex.stockActual : null;

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">Kardex y ajustes de inventario</h1>
        {kardex && (
          <Button onClick={() => setAjusteAbierto(true)}>Registrar ajuste</Button>
        )}
      </div>

      <p className="text-slate-600">
        Historial de entradas, salidas y ajustes del almacén principal. Los movimientos no se
        editan ni se borran: un error se corrige con un ajuste nuevo.
      </p>

      <form onSubmit={buscar} className="rounded border bg-white p-4">
        <fieldset disabled={buscando} className="flex flex-wrap items-end gap-3">
          <div className="min-w-64 flex-1 space-y-2">
            <Label htmlFor="kardex-busqueda">Producto</Label>
            <Input
              id="kardex-busqueda"
              type="search"
              maxLength={100}
              placeholder="Nombre o código, por ejemplo: olla"
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
            />
          </div>
          <Button type="submit">{buscando ? "Buscando…" : "Buscar"}</Button>
        </fieldset>

        {resultados && (
          <ul className="mt-4 divide-y rounded border" aria-label="Productos encontrados">
            {resultados.length === 0 && (
              <li className="p-3 text-slate-600">No se encontraron productos.</li>
            )}
            {resultados.map((p) => (
              <li key={p.productoId}>
                <button
                  type="button"
                  className="flex w-full justify-between gap-3 p-3 text-left hover:bg-slate-50"
                  onClick={() => seleccionar(p.productoId)}
                >
                  <span>
                    <span className="font-medium">{p.codigo}</span> · {p.nombre}
                  </span>
                  <span className="text-slate-600">
                    Stock: {cantidad.format(p.cantidad)} {p.unidadMedida}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </form>

      {productoId === null && !resultados && (
        <p className="text-slate-600">Busque un producto para ver su kardex.</p>
      )}

      {cargando && <p role="status">Consultando kardex…</p>}

      {error && (
        <div role="alert" className="space-y-3 rounded bg-red-50 p-3">
          <p className="text-red-700">{error}</p>
          <Button variant="outline" onClick={recargar}>Reintentar</Button>
        </div>
      )}

      {kardex && !error && (
        <>
          <div className="grid gap-3 rounded border bg-white p-4 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <p className="text-sm text-slate-600">Producto</p>
              <p className="font-medium">{kardex.codigo} · {kardex.nombre}</p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Stock actual</p>
              <p className="text-xl font-semibold">
                {cantidad.format(kardex.stockActual)} {kardex.unidadMedida}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Entradas / salidas del periodo</p>
              <p className="font-medium">
                +{cantidad.format(kardex.totalEntradas)} / −{cantidad.format(kardex.totalSalidas)}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-600">Cuadre con stock</p>
              {cuadra === null ? (
                <p className="text-slate-600">No aplica con fecha final</p>
              ) : (
                <span
                  className={
                    cuadra
                      ? "rounded bg-green-100 px-2 py-1 text-green-900"
                      : "rounded bg-red-100 px-2 py-1 text-red-900"
                  }
                >
                  {cuadra ? "Cuadra" : "No cuadra: revise el inventario"}
                </span>
              )}
            </div>
          </div>

          <form onSubmit={filtrar} className="flex flex-wrap items-end gap-3">
            <div className="space-y-2">
              <Label htmlFor="kardex-desde">Desde</Label>
              <Input id="kardex-desde" type="date" value={desde} onChange={(e) => setDesde(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="kardex-hasta">Hasta</Label>
              <Input id="kardex-hasta" type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} />
            </div>
            <Button type="submit" variant="outline">Filtrar</Button>
            <Button type="button" variant="ghost" onClick={limpiarFiltros}>Ver todo</Button>
          </form>

          <div className="overflow-x-auto rounded border">
            <table className="w-full text-left">
              <caption className="sr-only">Kardex de {kardex.nombre}</caption>
              <thead className="bg-slate-50">
                <tr>
                  <th scope="col" className="px-3 py-3">Fecha</th>
                  <th scope="col" className="px-3 py-3">Referencia</th>
                  <th scope="col" className="px-3 py-3 text-right">Entrada</th>
                  <th scope="col" className="px-3 py-3 text-right">Salida</th>
                  <th scope="col" className="px-3 py-3 text-right">Saldo</th>
                  <th scope="col" className="px-3 py-3">Empleado</th>
                  <th scope="col" className="px-3 py-3">Observación</th>
                </tr>
              </thead>
              <tbody>
                {filtros.desde && (
                  <tr className="border-t bg-slate-50">
                    <td className="px-3 py-3" colSpan={4}>Saldo inicial al {filtros.desde}</td>
                    <td className="px-3 py-3 text-right font-medium">{cantidad.format(kardex.saldoInicial)}</td>
                    <td colSpan={2} />
                  </tr>
                )}
                {kardex.movimientos.map((m) => (
                  <tr key={m.movimientoId} className={m.tipo === "Ajuste" ? "border-t bg-amber-50" : "border-t"}>
                    <td className="whitespace-nowrap px-3 py-3">{fechaHora.format(new Date(m.fecha))}</td>
                    <td className="px-3 py-3">{m.referencia}</td>
                    <td className="px-3 py-3 text-right">{m.entrada ? cantidad.format(m.entrada) : ""}</td>
                    <td className="px-3 py-3 text-right">{m.salida ? cantidad.format(m.salida) : ""}</td>
                    <td className="px-3 py-3 text-right font-medium">{cantidad.format(m.saldo)}</td>
                    <td className="px-3 py-3">{m.empleado}</td>
                    <td className="px-3 py-3 text-slate-600">{m.observacion}</td>
                  </tr>
                ))}
                {kardex.movimientos.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-3 py-6 text-center">
                      Sin movimientos en este periodo.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <AjusteDialog
            abierto={ajusteAbierto}
            kardex={kardex}
            onCerrar={() => setAjusteAbierto(false)}
            onRegistrado={() => {
              setAjusteAbierto(false);
              recargar();
            }}
          />
        </>
      )}
    </section>
  );
}

interface AjusteDialogProps {
  abierto: boolean;
  kardex: Kardex;
  onCerrar: () => void;
  onRegistrado: () => void;
}

function AjusteDialog({ abierto, kardex, onCerrar, onRegistrado }: AjusteDialogProps) {
  const [motivo, setMotivo] = useState<Motivo>("Merma");
  const [valor, setValor] = useState("");
  const [observacion, setObservacion] = useState("");
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!abierto) return;
    setMotivo("Merma");
    setValor("");
    setObservacion("");
    setError("");
  }, [abierto]);

  const esConteo = motivo === "ConteoFisico";
  const numero = Number(valor);
  const valido = valor !== "" && Number.isFinite(numero);
  const resultante = valido ? (esConteo ? numero : kardex.stockActual - numero) : null;

  async function guardar(event: SubmitEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setGuardando(true);
    setError("");

    try {
      const ajuste = await api<AjusteRegistrado>("/inventario/ajustes", {
        method: "POST",
        body: JSON.stringify({
          productoId: kardex.productoId,
          motivo,
          cantidad: numero,
          observacion: observacion.trim() || null,
        }),
      });

      const signo = ajuste.diferencia > 0 ? "+" : "";
      toast.success(
        `Ajuste registrado (${signo}${cantidad.format(ajuste.diferencia)}). Stock: ${cantidad.format(ajuste.stockNuevo)}.`,
      );
      onRegistrado();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo registrar el ajuste.");
    } finally {
      setGuardando(false);
    }
  }

  return (
    <Dialog open={abierto} onOpenChange={(open) => { if (!open) onCerrar(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Registrar ajuste</DialogTitle>
          <DialogDescription>
            {kardex.codigo} · {kardex.nombre}. Stock actual: {cantidad.format(kardex.stockActual)}{" "}
            {kardex.unidadMedida}.
          </DialogDescription>
        </DialogHeader>

        <form id="form-ajuste" onSubmit={guardar} className="space-y-4">
          {error && <p role="alert" className="rounded bg-red-50 p-2 text-red-700">{error}</p>}

          <div className="space-y-2">
            <Label htmlFor="ajuste-motivo">Motivo</Label>
            <select
              id="ajuste-motivo"
              className="h-9 w-full rounded border px-3"
              value={motivo}
              onChange={(e) => setMotivo(e.target.value as Motivo)}
            >
              {motivos.map((m) => (
                <option key={m.valor} value={m.valor}>{m.texto}</option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="ajuste-cantidad">
              {esConteo ? "Cantidad contada en el almacén" : "Cantidad a descontar"}
            </Label>
            <Input
              id="ajuste-cantidad"
              type="number"
              inputMode="decimal"
              min={esConteo ? 0 : 0.001}
              max={esConteo ? undefined : kardex.stockActual}
              step="0.001"
              required
              value={valor}
              onChange={(e) => setValor(e.target.value)}
            />
            {resultante !== null && (
              <p className={resultante < 0 ? "text-sm text-red-700" : "text-sm text-slate-600"}>
                Stock resultante: {cantidad.format(resultante)}
                {resultante < 0 && " (no permitido)"}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="ajuste-observacion">Observación (opcional)</Label>
            <textarea
              id="ajuste-observacion"
              className="min-h-20 w-full rounded border px-3 py-2"
              maxLength={300}
              value={observacion}
              onChange={(e) => setObservacion(e.target.value)}
            />
          </div>
        </form>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onCerrar}>Cancelar</Button>
          <Button
            type="submit"
            form="form-ajuste"
            disabled={guardando || !valido || (resultante !== null && resultante < 0)}
          >
            {guardando ? "Guardando…" : "Registrar ajuste"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
