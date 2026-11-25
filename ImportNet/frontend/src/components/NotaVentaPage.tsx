import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { fechaHora, moneda } from "@/lib/format";
import type { NotaVenta } from "@/lib/types";
import { Button } from "@/components/ui/button";

export default function NotaVentaPage() {
  const [nota, setNota] = useState<NotaVenta | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    const value = new URLSearchParams(window.location.search).get("id");
    const id = Number(value);
    if (!value || !Number.isSafeInteger(id) || id <= 0) {
      setError("La nota solicitada no es válida.");
      return;
    }
    const controller = new AbortController();
    api<NotaVenta>(`/ventas/${id}`, { signal: controller.signal })
      .then(result => { if (!controller.signal.aborted) setNota(result); })
      .catch(error => {
        if (!controller.signal.aborted) setError(error instanceof Error ? error.message : "No se pudo cargar la nota.");
      });
    return () => controller.abort();
  }, []);

  if (error) return <section><h1 className="text-2xl font-semibold">Nota de venta</h1><p role="alert" className="mt-4">{error}</p><a className="mt-4 inline-block underline" href="/ventas/historial">Volver al historial</a></section>;
  if (!nota) return <p role="status">Cargando nota de venta…</p>;

  const { venta, lineas } = nota;
  return <section className="nota-venta mx-auto max-w-4xl">
    <div className="no-print mb-6 flex flex-wrap gap-3">
      <Button variant="outline" asChild><a href="/ventas/historial">Volver al historial</a></Button>
      <Button onClick={() => window.print()}>Imprimir nota</Button>
    </div>
    <article className="rounded-lg border bg-white p-6 md:p-10">
      <header className="flex flex-wrap justify-between gap-4 border-b pb-6">
        <div><h1 className="text-2xl font-semibold">ImportNet</h1><p className="mt-2">Nota de venta</p></div>
        <div><p className="font-semibold">N.º {String(venta.numero).padStart(8, "0")}</p>
          <p className="mt-2">{fechaHora(venta.fecha)}</p><p className="mt-2">Estado: {venta.estado}</p></div>
      </header>
      <div className="my-6 space-y-2">
        <p><strong>Vendedor:</strong> {venta.vendedor}</p>
        <p><strong>Cliente:</strong> {venta.cliente}</p>
        {venta.documentoCliente && <p><strong>Documento:</strong> {venta.documentoCliente}</p>}
      </div>
      {venta.estado === "Anulada" && <div className="mb-6 border border-red-400 p-4 text-red-800">
        <p className="font-semibold">VENTA ANULADA</p><p className="mt-1">{venta.motivoAnulacion}</p>
      </div>}
      <div className="overflow-x-auto"><table className="w-full border-collapse text-left text-sm">
        <caption className="sr-only">Detalle de la venta {venta.numero}</caption>
        <thead><tr className="border-b">
          <th scope="col" className="py-3 pr-3">Producto</th><th scope="col" className="py-3 pr-3">Cantidad</th>
          <th scope="col" className="py-3 pr-3">Precio</th><th scope="col" className="py-3 pr-3">Descuento</th><th scope="col" className="py-3 text-right">Subtotal</th>
        </tr></thead>
        <tbody>{lineas.map((linea, index) => <tr key={`${linea.codigo}-${index}`} className="border-b">
          <td className="py-3 pr-3"><p>{linea.producto}</p><p className="text-xs text-slate-500">{linea.codigo}</p></td>
          <td className="py-3 pr-3">{linea.cantidad} {linea.unidadMedida}</td>
          <td className="py-3 pr-3">{moneda(linea.precioUnitario)}</td><td className="py-3 pr-3">{moneda(linea.descuento)}</td>
          <td className="py-3 text-right">{moneda(linea.subtotal)}</td>
        </tr>)}</tbody>
      </table></div>
      <dl className="ml-auto mt-8 grid max-w-xs grid-cols-2 gap-3">
        <dt>Subtotal</dt><dd className="text-right">{moneda(venta.subtotal)}</dd>
        <dt>Descuento</dt><dd className="text-right">{moneda(venta.descuento)}</dd>
        <dt className="text-lg font-semibold">Total</dt><dd className="text-right text-lg font-semibold">{moneda(venta.total)}</dd>
      </dl>
      <p className="mt-10 text-xs text-slate-600">Documento interno de venta. No constituye comprobante electrónico SUNAT.</p>
    </article>
  </section>;
}
