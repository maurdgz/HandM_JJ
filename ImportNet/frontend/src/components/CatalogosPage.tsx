import { useEffect, useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
type Row = { id: number; nombre: string; [key: string]: unknown };
type Modulo = "productos" | "categorias" | "proveedores" | "clientes";
const campos: Record<Modulo, string[]> = {
 productos: ["codigo", "nombre", "descripcion", "unidadMedida", "precioVenta", "categoriaId", "stockMinimo"],
 categorias: ["nombre"], proveedores: ["nombre", "tipo", "pais", "contacto"], clientes: ["tipoDocumento", "numeroDocumento", "nombre", "telefono"]
};
const etiquetas: Record<string, string> = {
 codigo: "SKU", nombre: "Nombre", descripcion: "Descripción", unidadMedida: "Unidad de medida", precioVenta: "Precio de venta (PEN)",
 categoriaId: "Categoría", stockMinimo: "Stock mínimo", tipo: "Tipo de proveedor", pais: "País", contacto: "Contacto",
 tipoDocumento: "Tipo de documento", numeroDocumento: "Número de documento", telefono: "Teléfono"
};
const titulos: Record<Modulo, [string, string]> = {
 productos: ["Productos", "producto"], categorias: ["Categorías", "categoría"], proveedores: ["Proveedores", "proveedor"], clientes: ["Clientes", "cliente"]
};
const opciones: Record<string, [number, string][]> = { tipo: [[0, "Nacional"], [1, "Internacional"]], tipoDocumento: [[0, "DNI"], [1, "RUC"]] };
const numeros = ["precioVenta", "categoriaId", "stockMinimo", "tipo", "tipoDocumento"];
const opcionales = ["descripcion", "telefono"];
const selectClass = "h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm shadow-xs";
export default function CatalogosPage({ modulo, admin }: { modulo: Modulo; admin: boolean }) {
 const [rows, setRows] = useState<Row[]>([]), [texto, setTexto] = useState("");
 const [pagina, setPagina] = useState(1), [tamano, setTamano] = useState(20), [count, setCount] = useState(0), [bajo, setBajo] = useState(false), [categoria, setCategoria] = useState("");
 const [cats, setCats] = useState<Row[]>([]), [productos, setProductos] = useState<Row[]>([]);
 const [draft, setDraft] = useState<Record<string, unknown> | null>(null), [error, setError] = useState(""), [busy, setBusy] = useState(false);
 const fail = (e: unknown) => setError(e instanceof Error ? e.message : "Error inesperado");
 const [titulo, singular] = titulos[modulo];
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
 function nuevo() {
  setError("");
  setDraft(Object.fromEntries([...campos[modulo].map(k => [k, numeros.includes(k) ? 0 : ""]), ...(modulo === "proveedores" ? [["productos", []], ["activo", true]] : [])]));
 }
 function campo(k: string) {
  const id = `campo-${k}`, valor = String(draft?.[k] ?? ""), cambiar = (v: string) => setDraft({...draft, [k]: v});
  let control;
  if (k === "categoriaId") control = <select id={id} className={selectClass} required value={valor === "0" ? "" : valor} onChange={e => cambiar(e.target.value)}><option value="">Seleccionar</option>{cats.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select>;
  else if (opciones[k]) control = <select id={id} className={selectClass} value={valor} onChange={e => cambiar(e.target.value)}>{opciones[k].map(([v, t]) => <option key={v} value={v}>{t}</option>)}</select>;
  else control = <Input id={id} required={!opcionales.includes(k)} type={numeros.includes(k) ? "number" : "text"} min={numeros.includes(k) ? 0 : undefined} step="any" value={valor} onChange={e => cambiar(e.target.value)} />;
  return <div key={k} className="space-y-1.5"><Label htmlFor={id}>{etiquetas[k] ?? k}</Label>{control}</div>;
 }
 return <section className="space-y-5">
  <div className="flex flex-wrap items-center justify-between gap-3">
   <h1 className="text-2xl font-semibold">{titulo}</h1>
   {canCreate && <Button onClick={nuevo}>Nuevo {singular}</Button>}
  </div>
  {modulo === "productos" && <p className="text-sm text-muted-foreground">Precios de venta en PEN.</p>}
  {modulo === "clientes" && <p className="text-sm text-muted-foreground">Clientes locales: DNI (8 dígitos), RUC (11 dígitos).</p>}
  {error && !draft && <p role="alert" className="text-red-700">{error}</p>}
  <Input aria-label="Buscar" placeholder={modulo === "productos" ? "Buscar por nombre o SKU" : modulo === "clientes" ? "Buscar por nombre o documento" : "Buscar por nombre"} value={texto} onChange={e => {setTexto(e.target.value); setPagina(1);}} />
  {modulo === "productos" && <div className="flex flex-wrap items-center gap-4">
   <select aria-label="Categoría" className={`${selectClass} max-w-xs`} value={categoria} onChange={e => {setCategoria(e.target.value);setPagina(1);}}><option value="">Todas las categorías</option>{cats.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</select>
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={bajo} onChange={e => {setBajo(e.target.checked);setPagina(1);}} /> Solo stock bajo mínimo</label>
  </div>}
  <Dialog open={draft !== null} onOpenChange={open => { if (!open) { setDraft(null); setError(""); } }}>
   <DialogContent className="max-h-[90vh] overflow-y-auto">
    <DialogHeader>
     <DialogTitle>{draft?.id ? `Editar ${singular}` : `Nuevo ${singular}`}</DialogTitle>
     <DialogDescription>{modulo === "productos" ? "El SKU debe ser único. El precio de venta se registra en soles." : "Complete los datos y guarde."}</DialogDescription>
    </DialogHeader>
    {draft && <form id="form-catalogo" onSubmit={save} className="space-y-4">
     {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
     {campos[modulo].map(campo)}
     {modulo === "proveedores" && <>
      <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={Boolean(draft.activo)} onChange={e => setDraft({...draft, activo: e.target.checked})} /> Activo</label>
      <fieldset className="space-y-1"><legend className="text-sm font-medium">Productos suministrados</legend>{productos.map(p => <label key={p.id} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={(draft.productos as number[]).includes(p.id)} onChange={e => setDraft({...draft, productos: e.target.checked ? [...draft.productos as number[], p.id] : (draft.productos as number[]).filter(x => x !== p.id)})} />{p.nombre}</label>)}</fieldset>
     </>}
    </form>}
    <DialogFooter>
     <Button type="button" variant="outline" onClick={() => { setDraft(null); setError(""); }}>Cancelar</Button>
     <Button type="submit" form="form-catalogo" disabled={busy}>{busy ? "Guardando…" : "Guardar"}</Button>
    </DialogFooter>
   </DialogContent>
  </Dialog>
  <Table>
   <TableHeader><TableRow><TableHead>Nombre</TableHead><TableHead>Datos</TableHead>{admin && <TableHead className="text-right">Acciones</TableHead>}</TableRow></TableHeader>
   <TableBody>
    {rows.length === 0 && <TableRow><TableCell colSpan={3} className="text-center text-muted-foreground">Sin resultados.</TableCell></TableRow>}
    {rows.map(row => <TableRow key={row.id}>
     <TableCell className="font-medium">{row.nombre}</TableCell>
     <TableCell>{modulo === "productos" ? `${row.codigo} · PEN ${row.precioVenta}` : modulo === "clientes" ? String(row.numeroDocumento) : modulo === "proveedores" ? String(row.pais) : row.id}</TableCell>
     {admin && <TableCell className="space-x-2 text-right">
      <Button size="sm" variant="outline" onClick={() => void edit(row)}>Editar</Button>
      <Button size="sm" variant="outline" onClick={async () => {if (!window.confirm(`¿Eliminar ${singular} "${row.nombre}"?`)) return; try {setError(""); await api(`/${modulo}/${row.id}`, {method: "DELETE"}); await load();} catch(e) {fail(e);}}}>Eliminar</Button>
     </TableCell>}
    </TableRow>)}
   </TableBody>
  </Table>
  {modulo === "productos" && <div className="flex items-center gap-3"><Button variant="outline" disabled={pagina === 1} onClick={() => setPagina(pagina-1)}>Anterior</Button><span className="text-sm">Página {pagina} de {Math.max(1, Math.ceil(count / tamano))} · {count} resultados</span><Button variant="outline" disabled={pagina*tamano >= count} onClick={() => setPagina(pagina+1)}>Siguiente</Button></div>}
 </section>;
}
