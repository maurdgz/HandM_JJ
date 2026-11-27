import { useCallback, useEffect, useState, type SubmitEvent } from "react";
import { toast } from "sonner";

import { api } from "@/lib/api";
import { pen, type Pagina } from "@/lib/compras";
import { fechaHora, numeroVenta, type ResumenVenta } from "@/lib/ventas";
import AnularVentaDialog, { type VentaAAnular } from "@/components/AnularVentaDialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const TAMANO = 20;

function hoyEnLima(): string {
  // en-CA formatea como AAAA-MM-DD.
  return new Intl.DateTimeFormat("en-CA", { timeZone: "America/Lima" }).format(new Date());
}

interface Props {
  admin: boolean;
}

export default function HistorialVentasPage({ admin }: Props) {
  const hoy = hoyEnLima();
  const [desde, setDesde] = useState(hoy);
  const [hasta, setHasta] = useState(hoy);
  const [filtros, setFiltros] = useState({ desde: hoy, hasta: hoy });
  const [pagina, setPagina] = useState(1);

  const [datos, setDatos] = useState<Pagina<ResumenVenta> | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState("");
  const [recarga, setRecarga] = useState(0);
  const [aAnular, setAAnular] = useState<VentaAAnular | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const parametros = new URLSearchParams({ pagina: String(pagina), tamano: String(TAMANO) });
    if (filtros.desde) parametros.set("desde", filtros.desde);
    if (filtros.hasta) parametros.set("hasta", filtros.hasta);

    setCargando(true);
    setError("");

    api<Pagina<ResumenVenta>>(`/ventas?${parametros.toString()}`, { signal: controller.signal })
      .then(setDatos)
      .catch((e: unknown) => {
        if (controller.signal.aborted) return;
        setDatos(null);
        setError(e instanceof Error ? e.message : "No se pudo consultar el historial.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setCargando(false);
      });

    return () => controller.abort();
  }, [filtros, pagina, recarga]);

  const recargar = useCallback(() => setRecarga((n) => n + 1), []);

  function filtrar(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (desde && hasta && desde > hasta) {
      toast.error("La fecha inicial no puede ser posterior a la final.");
      return;
    }

    setPagina(1);
    setFiltros({ desde, hasta });
  }

  function verTodo(): void {
    setDesde("");
    setHasta("");
    setPagina(1);
    setFiltros({ desde: "", hasta: "" });
  }

  const totalPaginas = datos ? Math.max(1, Math.ceil(datos.count / TAMANO)) : 1;
  const totalRegistradas = datos?.items
    .filter((v) => v.estado === "Registrada")
    .reduce((s, v) => s + v.total, 0) ?? 0;

  return (
    <section className="space-y-5">
      <h1 className="text-2xl font-semibold">Historial de ventas</h1>

      <p className="text-slate-600">
        {admin
          ? "Ventas de todos los vendedores. Solo el Administrador puede anular, con motivo."
          : "Solo se muestran tus ventas."}
      </p>

      <form onSubmit={filtrar} className="flex flex-wrap items-end gap-3 rounded border bg-white p-4">
        <div className="space-y-2">
          <Label htmlFor="historial-desde">Desde</Label>
          <Input id="historial-desde" type="date" value={desde} onChange={(e) => setDesde(e.target.value)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="historial-hasta">Hasta</Label>
          <Input id="historial-hasta" type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} />
        </div>
        <Button type="submit" variant="outline">Filtrar</Button>
        <Button type="button" variant="ghost" onClick={verTodo}>Ver todo</Button>
      </form>

      {cargando && <p role="status">Consultando ventas…</p>}

      {error && (
        <div role="alert" className="space-y-3 rounded bg-red-50 p-3">
          <p className="text-red-700">{error}</p>
          <Button variant="outline" onClick={recargar}>Reintentar</Button>
        </div>
      )}

      {datos && !error && (
        <>
          <p className="text-slate-600">
            {datos.count} venta(s). Total registrado en esta página:{" "}
            <span className="font-semibold text-slate-900">{pen(totalRegistradas)}</span>
          </p>

          <div className="overflow-x-auto rounded border">
            <table className="w-full text-left">
              <caption className="sr-only">Historial de ventas</caption>
              <thead className="bg-slate-50">
                <tr>
                  <th scope="col" className="px-3 py-3">Número</th>
                  <th scope="col" className="px-3 py-3">Fecha</th>
                  {admin && <th scope="col" className="px-3 py-3">Vendedor</th>}
                  <th scope="col" className="px-3 py-3">Cliente</th>
                  <th scope="col" className="px-3 py-3 text-right">Líneas</th>
                  <th scope="col" className="px-3 py-3 text-right">Total</th>
                  <th scope="col" className="px-3 py-3">Estado</th>
                  <th scope="col" className="px-3 py-3"><span className="sr-only">Acciones</span></th>
                </tr>
              </thead>
              <tbody>
                {datos.items.map((v) => (
                  <tr key={v.id} className={v.estado === "Anulada" ? "border-t bg-slate-50 text-slate-500" : "border-t"}>
                    <td className="whitespace-nowrap px-3 py-3 font-medium">{numeroVenta(v.numero)}</td>
                    <td className="whitespace-nowrap px-3 py-3">{fechaHora.format(new Date(v.fecha))}</td>
                    {admin && <td className="px-3 py-3">{v.vendedor}</td>}
                    <td className="px-3 py-3">{v.cliente}</td>
                    <td className="px-3 py-3 text-right">{v.lineas}</td>
                    <td className={v.estado === "Anulada" ? "px-3 py-3 text-right line-through" : "px-3 py-3 text-right"}>
                      {pen(v.total)}
                    </td>
                    <td className="px-3 py-3">
                      <span
                        className={
                          v.estado === "Anulada"
                            ? "rounded bg-red-100 px-2 py-1 text-sm text-red-900"
                            : "rounded bg-green-100 px-2 py-1 text-sm text-green-900"
                        }
                      >
                        {v.estado}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-3 py-3 text-right">
                      <a
                        href={`/nota?id=${v.id}`}
                        className="mr-2 inline-flex h-9 items-center rounded border px-3 text-sm hover:bg-slate-100"
                      >
                        Ver nota
                      </a>
                      {admin && v.estado === "Registrada" && (
                        <Button
                          variant="outline"
                          className="text-red-700"
                          onClick={() => setAAnular({ id: v.id, numero: v.numero, total: v.total })}
                        >
                          Anular
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
                {datos.items.length === 0 && (
                  <tr>
                    <td colSpan={admin ? 8 : 7} className="px-3 py-6 text-center">
                      No hay ventas en este periodo.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {totalPaginas > 1 && (
            <nav aria-label="Paginación" className="flex items-center gap-3">
              <Button variant="outline" disabled={pagina <= 1 || cargando} onClick={() => setPagina((p) => p - 1)}>
                Anterior
              </Button>
              <span>Página {pagina} de {totalPaginas}</span>
              <Button variant="outline" disabled={pagina >= totalPaginas || cargando} onClick={() => setPagina((p) => p + 1)}>
                Siguiente
              </Button>
            </nav>
          )}
        </>
      )}

      {admin && (
        <AnularVentaDialog
          venta={aAnular}
          onCerrar={() => setAAnular(null)}
          onAnulada={() => {
            setAAnular(null);
            recargar();
          }}
        />
      )}
    </section>
  );
}
