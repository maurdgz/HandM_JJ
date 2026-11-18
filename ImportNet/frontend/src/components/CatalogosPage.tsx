import { useEffect, useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
type Row = { id: number; nombre: string; [key: string]: unknown };
type Modulo = "productos" | "categorias" | "proveedores" | "clientes";
const campos: Record<Modulo, string[]> = {
 productos: ["codigo", "nombre", "descripcion", "unidadMedida", "precioVenta", "categoriaId", "stockMinimo"],
 categorias: ["nombre"], proveedores: ["nombre", "tipo", "pais", "contacto"], clientes: ["tipoDocumento", "numeroDocumento", "nombre", "telefono"]
};
const numeros = ["precioVenta", "categoriaId", "stockMinimo", "tipo", "tipoDocumento"];
export default function CatalogosPage({ modulo, admin }: { modulo: Modulo; admin: boolean }) {
 const [rows, setRows] = useState<Row[]>([]), [texto, setTexto] = useState("");
 const [pagina, setPagina] = useState(1), [tamano, setTamano] = useState(20), [count, setCount] = useState(0), [bajo, setBajo] = useState(false), [categoria, setCategoria] = useState("");
 const [cats, setCats] = useState<Row[]>([]), [productos, setProductos] = useState<Row[]>([]);
 const [draft, setDraft] = useState<Record<string, unknown> | null>(null), [error, setError] = useState(""), [busy, setBusy] = useState(false);
 const fail = (e: unknown) => setError(e instanceof Error ? e.message : "Error inesperado");
 async function load() {
  try {
   if (modulo === "productos") { const p = await api<{items: Row[]; count: number; tamano: number}>(`/productos/buscar?texto=${encodeURIComponent(texto)}&pagina=${pagina}&stockBajo=${bajo}${categoria ? `&categoriaId=${categoria}` : ""}`); setRows(p.items); setCount(p.count); setTamano(p.tamano); }
   else setRows(await api<Row[]>(`/${modulo}?texto=${encodeURIComponent(texto)}`));
  } catch (e) { fail(e); }
 }
 useEffect(() => { void load(); }, [modulo, texto, pagina, bajo, categoria]);
 useEffect(() => { if (modulo === "productos") api<Row[]>("/categorias").then(setCats).catch(fail); if (modulo === "proveedores") api<Row[]>("/productos").then(setProductos).catch(fail); }, [modulo]);
 if (modulo === "proveedores" && !admin) return <p role="alert">Acceso restringido a Administrador.</p>;
 const canCreate = admin || modulo === "clientes";
 async function save(e: FormEvent) {
  e.preventDefault(); if (!draft) return; setBusy(true); setError("");
  try {
   const data = {...draft}; numeros.forEach(k => { if (k in data) data[k] = Number(data[k]); });
   // Peso and partida are not editable here: keep the stored values on edit, send null on create.
   if (modulo === "productos") { data.peso ??= null; data.partidaArancelaria ??= null; }
   await api(`/${modulo}${draft.id ? `/${draft.id}` : ""}`, {method: draft.id ? "PUT" : "POST", body: JSON.stringify(data)});
   setDraft(null); await load();
  } catch (e) { fail(e); } finally { setBusy(false); }
 }
 async function edit(row: Row) {
  try {
   if (modulo === "proveedores") { const d = await api<{proveedor: Row; productos: number[]}>(`/proveedores/${row.id}`); setDraft({...d.proveedor, productos: d.productos}); }
   else setDraft({...row});
  } catch(e) { fail(e); }
 }
 return <section className="space-y-5">
  <h1 className="text-2xl font-semibold capitalize">{modulo}</h1>
  <p>Precios de venta en PEN. Clientes locales: DNI (8 dígitos), RUC (11 dígitos).</p>
  {error && <p role="alert" className="text-red-700">{error}</p>}
  <Input aria-label="Buscar" placeholder="Buscar por nombre o documento/SKU" value={texto} onChange={e => {setTexto(e.target.value); setPagina(1);}} />
  {modulo === "productos" && <div className="flex gap-4"><select aria-label="Categoría" value={categoria} onChange={e => {setCategoria(e.target.value);setPagina(1);}}><option value="">Todas las categorías</option>{cats.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select><label><input type="checkbox" checked={bajo} onChange={e => {setBajo(e.target.checked);setPagina(1);}} /> Stock bajo</label></div>}
  {canCreate && <Button onClick={() => setDraft(Object.fromEntries([...campos[modulo].map(k => [k, numeros.includes(k) ? 0 : ""]), ...(modulo === "proveedores" ? [["productos", []], ["activo", true]] : [])]))}>Nuevo registro</Button>}
  {draft && <form onSubmit={save} className="space-y-3 rounded border p-4">
   {campos[modulo].map(k => <label key={k} className="block">{k}{k === "categoriaId" ? <select required value={String(draft[k])} onChange={e => setDraft({...draft, [k]: e.target.value})}><option value="">Seleccionar</option>{cats.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select> : <Input required={!["descripcion", "telefono"].includes(k)} type={numeros.includes(k) ? "number" : "text"} step="any" value={String(draft[k] ?? "")} onChange={e => setDraft({...draft, [k]: e.target.value})} />}</label>)}
   {modulo === "proveedores" && <><p>Tipo: 0 nacional, 1 internacional</p><label><input type="checkbox" checked={Boolean(draft.activo)} onChange={e => setDraft({...draft, activo: e.target.checked})} />Activo</label><fieldset><legend>Productos suministrados</legend>{productos.map(p => <label key={p.id} className="block"><input type="checkbox" checked={(draft.productos as number[]).includes(p.id)} onChange={e => setDraft({...draft, productos: e.target.checked ? [...draft.productos as number[], p.id] : (draft.productos as number[]).filter(x => x !== p.id)})} />{p.nombre}</label>)}</fieldset></>}
   {modulo === "clientes" && <p>Tipo de documento: 0 DNI, 1 RUC</p>}
   <Button disabled={busy}>Guardar</Button> <Button type="button" variant="outline" onClick={() => setDraft(null)}>Cancelar</Button>
  </form>}
  <div className="overflow-x-auto"><table className="w-full text-left"><thead><tr><th>Nombre</th><th>Datos</th><th>Acciones</th></tr></thead><tbody>{rows.map(row => <tr key={row.id} className="border-b"><td className="p-3">{row.nombre}</td><td>{modulo === "productos" ? `${row.codigo} · PEN ${row.precioVenta}` : modulo === "clientes" ? String(row.numeroDocumento) : modulo === "proveedores" ? String(row.pais) : row.id}</td><td>{admin && <><Button variant="outline" onClick={() => void edit(row)}>Editar</Button> <Button variant="outline" onClick={async () => {if (!window.confirm("¿Eliminar registro?")) return; try {await api(`/${modulo}/${row.id}`, {method: "DELETE"}); await load();} catch(e) {fail(e);}}}>Eliminar</Button></>}</td></tr>)}</tbody></table></div>
  {modulo === "productos" && <div className="flex gap-3"><Button disabled={pagina === 1} onClick={() => setPagina(pagina-1)}>Anterior</Button><span>Página {pagina} · {count} resultados</span><Button disabled={pagina*tamano >= count} onClick={() => setPagina(pagina+1)}>Siguiente</Button></div>}
 </section>;
}
