import { type CompraDatos, pen, usd } from "@/lib/compras";
export default function ResumenCompra({ datos }: { datos: CompraDatos }) {
 const subtotal = datos.lineas.reduce((s,l) => s + l.cantidad * l.costoUnitarioUsd, 0);
 const mercaderia = Math.round((subtotal * datos.tipoCambio + Number.EPSILON) * 100) / 100;
 const total = mercaderia + datos.fletePen + datos.aduanaPen;
 return <aside className="space-y-2 rounded bg-slate-50 p-4" aria-label="Vista previa de costos">
  <h3 className="font-semibold">Vista previa estimada</h3>
  <p>Mercadería USD: {usd(subtotal)}</p><p>Mercadería convertida PEN: {pen(mercaderia)}</p>
  <p>Flete PEN: {pen(datos.fletePen)} · Aduana / agente PEN: {pen(datos.aduanaPen)}</p>
  <p className="font-semibold">Costo puesto en tienda PEN: {pen(total)}</p>
  <p className="text-sm">El servidor confirma los importes y reparte los centavos al guardar.</p>
 </aside>;
}
