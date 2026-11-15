import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { type Compra, type Pagina, pen, usd } from "@/lib/compras";
import { Button } from "@/components/ui/button";
export default function ComprasPage() {
 const [lista, setLista] = useState<Pagina<Compra>>({items: [], count: 0, paginaActual: 1, tamano: 20});
 const [pagina, setPagina] = useState(1), [detalle, setDetalle] = useState<Compra | null>(null), [error, setError] = useState(""), [loading, setLoading] = useState(false);
 const fail = (e: unknown) => setError(e instanceof Error ? e.message : "No se pudo completar la operaci?n.");
 async function load() { setLoading(true); try { setLista(await api<Pagina<Compra>>(`/compras?pagina=${pagina}`)); } catch(e) {fail(e);} finally {setLoading(false);} }
 useEffect(() => { void load(); }, [pagina]);
 async function consultar(id: number) { setError(""); try {setDetalle(await api<Compra>(`/compras/${id}`));} catch(e) {fail(e);} }
 return <section className="space-y-5">
  <h1 className="text-2xl font-semibold">Compras de importaci?n</h1>
  <p>Borradores ? Origen del costo en USD ? Costo puesto en tienda en PEN</p>
  {error && <p role="alert" className="text-red-700">{error}</p>}
  {loading && <p role="status">Cargando compras?</p>}
  <div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>Compra</th><th>Fecha</th><th>Proveedor</th><th>Estado</th><th>Total PEN</th><th>Acciones</th></tr></thead><tbody>{lista.items.map(c => <tr key={c.id} className="border-b"><td className="py-3">#{c.id}</td><td>{c.fecha.slice(0,10)}</td><td>#{c.proveedorId}</td><td>{c.estado}</td><td>{pen(c.totalPen)}</td><td><Button variant="outline" onClick={() => void consultar(c.id)}>Consultar #{c.id}</Button></td></tr>)}</tbody></table></div>
  {!loading && lista.count === 0 && <p>No hay compras registradas.</p>}
  <div className="flex gap-3"><Button disabled={pagina === 1 || loading} onClick={() => setPagina(pagina-1)}>Anterior</Button><span>P?gina {pagina} ? {lista.count} compras</span><Button disabled={pagina*20 >= lista.count || loading} onClick={() => setPagina(pagina+1)}>Siguiente</Button></div>
  {detalle && <article className="space-y-3 rounded border p-4"><h2 className="text-xl">Compra #{detalle.id} ? {detalle.estado}</h2><p>Mercader?a: {usd(detalle.subtotalUsd)} ? {pen(detalle.mercaderiaPen)} ? TC {detalle.tipoCambio} PEN/USD</p><p>Flete: {pen(detalle.fletePen)} ? Aduana/agente: {pen(detalle.aduanaPen)} ? Total confirmado: {pen(detalle.totalPen)}</p><div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>Producto</th><th>Cantidad</th><th>Costo USD</th><th>Mercader?a PEN</th><th>Flete PEN</th><th>Aduana PEN</th><th>Total l?nea PEN</th><th>Unitario PEN</th></tr></thead><tbody>{detalle.lineas.map(l => <tr key={l.productoId}><td>#{l.productoId}</td><td>{l.cantidad}</td><td>{usd(l.costoUnitarioUsd)}</td><td>{pen(l.mercaderiaPen)}</td><td>{pen(l.fletePen)}</td><td>{pen(l.aduanaPen)}</td><td>{pen(l.totalPen)}</td><td>{pen(l.costoUnitarioPuestoPen)}</td></tr>)}</tbody></table></div><p>Los unitarios mostrados est?n redondeados; el total usa los importes exactos de l?nea.</p><Button variant="outline" onClick={() => setDetalle(null)}>Cerrar detalle</Button></article>}
 </section>;
}
