import { useEffect, useState, type SubmitEvent } from "react";
import { api } from "@/lib/api";
import type { CompraDatos, LineaCompra, Pagina } from "@/lib/compras";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
interface Opcion { id: number; nombre: string; activo: boolean }
interface Props {
 inicial?: CompraDatos; soloLectura?: boolean; busy?: boolean;
 onGuardar: (datos: CompraDatos) => Promise<void>; onCancelar: () => void;
 resumen?: (datos: CompraDatos) => React.ReactNode;
}
const nueva = (): CompraDatos => ({proveedorId: 0, fecha: new Date().toISOString(), tipoCambio: 0, fletePen: 0, aduanaPen: 0, lineas: [{productoId: 0, cantidad: 1, costoUnitarioUsd: 0}]});
export default function CompraForm({ inicial, soloLectura = false, busy = false, onGuardar, onCancelar, resumen }: Props) {
 const [datos, setDatos] = useState<CompraDatos>(() => inicial ? {...inicial, lineas: inicial.lineas.map(l => ({productoId: l.productoId, cantidad: l.cantidad, costoUnitarioUsd: l.costoUnitarioUsd}))} : nueva());
 const [proveedores, setProveedores] = useState<Opcion[]>([]), [productos, setProductos] = useState<Opcion[]>([]);
 const [buscarProveedor, setBuscarProveedor] = useState(""), [buscarProducto, setBuscarProducto] = useState(""), [error, setError] = useState("");
 useEffect(() => {
  let active = true;
  api<Opcion[]>(`/proveedores?texto=${encodeURIComponent(buscarProveedor)}`).then(p => { if(active) setProveedores(p.filter(x => x.activo)); }).catch(e => {if(active) setError(e instanceof Error ? e.message : "No se pudo cargar proveedores.");});
  return () => {active = false;};
 }, [buscarProveedor]);
 useEffect(() => {
  let active = true;
  api<Pagina<Opcion>>(`/productos/buscar?tamano=100&texto=${encodeURIComponent(buscarProducto)}`).then(p => { if(active) setProductos(p.items.filter(x => x.activo)); }).catch(e => {if(active) setError(e instanceof Error ? e.message : "No se pudo cargar productos.");});
  return () => {active = false;};
 }, [buscarProducto]);
 function linea(i: number, cambio: Partial<LineaCompra>) {setDatos(d => ({...d, lineas: d.lineas.map((l, index) => index === i ? {...l, ...cambio} : l)}));}
 async function submit(e: SubmitEvent<HTMLFormElement>) {
  e.preventDefault(); setError("");
  if (datos.lineas.length === 0 || new Set(datos.lineas.map(l => l.productoId)).size !== datos.lineas.length) { setError("Agrega l?neas sin repetir productos."); return; }
  try { await onGuardar(datos); } catch(e) {setError(e instanceof Error ? e.message : "No se pudo guardar.");}
 }
 return <form onSubmit={submit} className="space-y-5 rounded border bg-white p-4">
  <h2 className="text-xl font-semibold">{soloLectura ? "Consulta de compra recibida" : inicial ? "Editar borrador" : "Nueva compra en borrador"}</h2>
  {error && <p role="alert" className="text-red-700">{error}</p>}
  <fieldset disabled={soloLectura || busy} className="space-y-4">
   <div className="grid gap-4 md:grid-cols-2">
    <div><Label htmlFor="buscar-proveedor">Buscar proveedor</Label><Input id="buscar-proveedor" value={buscarProveedor} onChange={e => setBuscarProveedor(e.target.value)} /><Label htmlFor="proveedor">Proveedor</Label><select className="w-full rounded border p-2" id="proveedor" required value={datos.proveedorId || ""} onChange={e => setDatos({...datos, proveedorId: Number(e.target.value)})}><option value="">Selecciona proveedor activo</option>{datos.proveedorId > 0 && !proveedores.some(p => p.id === datos.proveedorId) && <option value={datos.proveedorId}>Proveedor #{datos.proveedorId}</option>}{proveedores.map(p => <option key={p.id} value={p.id}>{p.nombre}</option>)}</select></div>
    <div><Label htmlFor="fecha-compra">Fecha de compra</Label><Input id="fecha-compra" type="date" required value={datos.fecha.slice(0,10)} onChange={e => setDatos({...datos, fecha: `${e.target.value}T00:00:00Z`})} /></div>
    <div><Label htmlFor="tipo-cambio">Tipo de cambio manual (PEN por USD)</Label><Input id="tipo-cambio" type="number" required min="0.000001" max="1000" step="0.000001" value={datos.tipoCambio || ""} onChange={e => setDatos({...datos, tipoCambio: Number(e.target.value)})} /></div>
    <div><Label htmlFor="flete-pen">Flete PEN</Label><Input id="flete-pen" type="number" required min="0" max="1000000000000" step="0.01" value={datos.fletePen} onChange={e => setDatos({...datos, fletePen: Number(e.target.value)})} /></div>
    <div><Label htmlFor="aduana-pen">Aduana / agente PEN</Label><Input id="aduana-pen" type="number" required min="0" max="1000000000000" step="0.01" value={datos.aduanaPen} onChange={e => setDatos({...datos, aduanaPen: Number(e.target.value)})} /></div>
   </div>
   <div><Label htmlFor="buscar-producto">Buscar productos (nombre o SKU)</Label><Input id="buscar-producto" value={buscarProducto} onChange={e => setBuscarProducto(e.target.value)} /><p className="text-sm text-slate-600">Hasta 100 coincidencias; refina la b?squeda para encontrar otros productos.</p></div>
   <Table><TableHeader><TableRow><TableHead>Producto</TableHead><TableHead>Cantidad</TableHead><TableHead>Costo unitario USD</TableHead><TableHead>Acci?n</TableHead></TableRow></TableHeader><TableBody>{datos.lineas.map((l,i) => <TableRow key={i}>
    <TableCell><select aria-label={`Producto l?nea ${i+1}`} className="min-w-48 rounded border p-2" required value={l.productoId || ""} onChange={e => linea(i,{productoId: Number(e.target.value)})}><option value="">Selecciona producto</option>{l.productoId > 0 && !productos.some(p => p.id === l.productoId) && <option value={l.productoId}>Producto #{l.productoId}</option>}{productos.map(p => <option key={p.id} value={p.id} disabled={datos.lineas.some((otra,j) => j !== i && otra.productoId === p.id)}>{p.nombre}</option>)}</select></TableCell>
    <TableCell><Input aria-label={`Cantidad l?nea ${i+1}`} type="number" required min="0.001" max="1000000" step="0.001" value={l.cantidad || ""} onChange={e => linea(i,{cantidad: Number(e.target.value)})} /></TableCell>
    <TableCell><Input aria-label={`Costo USD l?nea ${i+1}`} type="number" required min="0.0001" max="1000000" step="0.0001" value={l.costoUnitarioUsd || ""} onChange={e => linea(i,{costoUnitarioUsd: Number(e.target.value)})} /></TableCell>
    <TableCell><Button type="button" variant="outline" aria-label={`Quitar l?nea ${i+1}`} onClick={() => setDatos({...datos, lineas: datos.lineas.filter((_,j) => j !== i)})}>Quitar</Button></TableCell>
   </TableRow>)}</TableBody></Table>
   <Button type="button" variant="outline" disabled={datos.lineas.length >= 100} onClick={() => setDatos({...datos, lineas: [...datos.lineas, {productoId: 0, cantidad: 1, costoUnitarioUsd: 0}]})}>Agregar l?nea</Button>
  </fieldset>
  {resumen?.(datos)}
  <p className="text-sm">Guardar un borrador no mueve stock ni actualiza costo promedio.</p>
  {!soloLectura && <Button disabled={busy || datos.lineas.length === 0} type="submit">{busy ? "Guardando?" : "Guardar borrador"}</Button>} <Button disabled={busy} variant="outline" type="button" onClick={onCancelar}>Cancelar</Button>
 </form>;
}
