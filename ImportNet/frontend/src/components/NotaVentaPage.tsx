import { useCallback, useEffect, useState } from "react";

import { api } from "@/lib/api";
import { pen } from "@/lib/compras";
import { cantidad, fechaHora, numeroVenta, type NotaVenta } from "@/lib/ventas";
import AnularVentaDialog, { type VentaAAnular } from "@/components/AnularVentaDialog";
import { Button } from "@/components/ui/button";

function ventaEnUrl(): number | null {
  const valor = Number(new URLSearchParams(window.location.search).get("id"));
  return Number.isSafeInteger(valor) && valor > 0 ? valor : null;
}

interface Props {
  admin: boolean;
}

export default function NotaVentaPage({ admin }: Props) {
  const [ventaId] = useState<number | null>(ventaEnUrl);
  const [nota, setNota] = useState<NotaVenta | null>(null);
  const [error, setError] = useState(ventaId === null ? "Indique una venta válida." : "");
  const [recarga, setRecarga] = useState(0);
  const [aAnular, setAAnular] = useState<VentaAAnular | null>(null);

  useEffect(() => {
    if (ventaId === null) return;

    const controller = new AbortController();
    setError("");

    api<NotaVenta>(`/ventas/${ventaId}`, { signal: controller.signal })
      .then((result) => { if (!controller.signal.aborted) setNota(result); })
      .catch((e: unknown) => {
        if (controller.signal.aborted) return;
        setNota(null);
        setError(e instanceof Error ? e.message : "No se pudo cargar la nota de venta.");
      });

    return () => controller.abort();
  }, [ventaId, recarga]);

  const recargar = useCallback(() => setRecarga((n) => n + 1), []);

  if (error) {
    return (
      <section className="space-y-3">
        <p role="alert" className="rounded bg-red-50 p-3 text-red-700">{error}</p>
        <a href="/historial" className="underline">Volver al historial</a>
      </section>
    );
  }

  if (!nota) return <p role="status">Cargando nota de venta…</p>;

  const anulada = nota.estado === "Anulada";

  return (
    <section className="space-y-5">
      <div className="no-print flex flex-wrap items-center justify-between gap-3">
        <a href="/historial" className="underline">← Historial de ventas</a>
        <div className="flex gap-2">
          {admin && !anulada && (
            <Button
              variant="outline"
              className="text-red-700"
              onClick={() => setAAnular({ id: nota.id, numero: nota.numero, total: nota.total })}
            >
              Anular venta
            </Button>
          )}
          <Button onClick={() => window.print()}>Imprimir</Button>
        </div>
      </div>

      <article className="nota-venta relative mx-auto max-w-2xl rounded border bg-white p-6 print:max-w-none print:border-0 print:p-0">
        {anulada && (
          <p className="mb-4 rounded border-2 border-red-700 p-3 text-center font-semibold text-red-700">
            ANULADA{nota.fechaAnulacion && ` el ${fechaHora.format(new Date(nota.fechaAnulacion))}`}
            {nota.motivoAnulacion && <span className="block font-normal">Motivo: {nota.motivoAnulacion}</span>}
          </p>
        )}

        <header className="flex flex-wrap justify-between gap-4 border-b pb-4">
          <div>
            <p className="text-xl font-semibold">ImportNet</p>
            <p className="text-sm text-slate-600">Nota de venta · No es comprobante electrónico SUNAT</p>
          </div>
          <div className="text-right">
            <p className="text-lg font-semibold">{numeroVenta(nota.numero)}</p>
            <p className="text-sm">{fechaHora.format(new Date(nota.fecha))}</p>
          </div>
        </header>

        <dl className="grid gap-2 py-4 sm:grid-cols-2">
          <div>
            <dt className="text-sm text-slate-600">Cliente</dt>
            <dd className="font-medium">{nota.cliente}</dd>
            {nota.clienteDocumento && (
              <dd className="text-sm">{nota.clienteTipoDocumento} {nota.clienteDocumento}</dd>
            )}
          </div>
          <div>
            <dt className="text-sm text-slate-600">Vendedor</dt>
            <dd className="font-medium">{nota.vendedor}</dd>
          </div>
        </dl>

        <table className="w-full text-left text-sm">
          <caption className="sr-only">Líneas de la venta</caption>
          <thead className="border-y">
            <tr>
              <th scope="col" className="py-2 pr-2">Producto</th>
              <th scope="col" className="py-2 pr-2 text-right">Cant.</th>
              <th scope="col" className="py-2 pr-2 text-right">P. unit.</th>
              <th scope="col" className="py-2 pr-2 text-right">Dscto.</th>
              <th scope="col" className="py-2 text-right">Importe</th>
            </tr>
          </thead>
          <tbody>
            {nota.lineas.map((l) => (
              <tr key={l.productoId} className="border-b">
                <td className="py-2 pr-2">
                  <span className="block">{l.producto}</span>
                  <span className="text-slate-600">{l.codigo}</span>
                </td>
                <td className="whitespace-nowrap py-2 pr-2 text-right">
                  {cantidad.format(l.cantidad)} {l.unidadMedida}
                </td>
                <td className="py-2 pr-2 text-right">{pen(l.precioUnitario)}</td>
                <td className="py-2 pr-2 text-right">{l.descuento ? pen(l.descuento) : ""}</td>
                <td className="py-2 text-right">{pen(l.subtotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <dl className="ml-auto mt-4 w-full max-w-60 space-y-1">
          <div className="flex justify-between">
            <dt>Subtotal</dt>
            <dd>{pen(nota.subtotal)}</dd>
          </div>
          <div className="flex justify-between">
            <dt>Descuento</dt>
            <dd>−{pen(nota.descuento)}</dd>
          </div>
          <div className="flex justify-between border-t pt-1 text-lg font-semibold">
            <dt>Total</dt>
            <dd className={anulada ? "line-through" : undefined}>{pen(nota.total)}</dd>
          </div>
        </dl>

        <p className="mt-6 text-center text-sm text-slate-600">Gracias por su compra.</p>
      </article>

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
