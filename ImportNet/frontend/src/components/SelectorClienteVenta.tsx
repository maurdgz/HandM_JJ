import { useEffect, useRef, useState } from "react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

interface ClienteVenta { id: number; nombre: string; numeroDocumento: string }

export default function SelectorClienteVenta({ cliente, onChange, disabled }: {
  cliente: ClienteVenta | null; onChange: (cliente: ClienteVenta | null, pendiente?: boolean) => void; disabled: boolean;
}) {
  const [documento, setDocumento] = useState("");
  const [error, setError] = useState("");
  const [cargando, setCargando] = useState(false);
  const solicitud = useRef<AbortController | null>(null);
  useEffect(() => () => solicitud.current?.abort(), []);
  async function buscar() {
    if (!/^(\d{8}|\d{11})$/.test(documento)) { setError("Ingresa un DNI de 8 o RUC de 11 dígitos."); return; }
    solicitud.current?.abort();
    const controller = new AbortController();
    solicitud.current = controller;
    setCargando(true); setError("");
    try {
      const encontrado = await api<ClienteVenta>(`/clientes/por-documento/${documento}`, { signal: controller.signal });
      if (!controller.signal.aborted) onChange(encontrado);
    } catch (e) {
      if (!controller.signal.aborted) setError(e instanceof Error ? e.message : "No se pudo buscar al cliente.");
    } finally { if (!controller.signal.aborted) setCargando(false); }
  }
  return <fieldset disabled={disabled} className="space-y-3 rounded-lg border p-4">
    <legend className="px-1 text-sm font-medium">Cliente de la venta</legend>
    <div className="flex flex-wrap items-end gap-2"><div className="min-w-0 flex-1"><label htmlFor="cliente-documento" className="text-sm">DNI o RUC (opcional)</label>
      <Input id="cliente-documento" inputMode="numeric" maxLength={11} value={documento} placeholder="Documento completo"
        onChange={e => { solicitud.current?.abort(); setCargando(false); setDocumento(e.target.value); setError(""); onChange(null, e.target.value.length > 0); }}
        onKeyDown={e => { if (e.key === "Enter") { e.preventDefault(); void buscar(); } }} />
    </div><Button variant="outline" disabled={cargando || !documento} onClick={() => void buscar()}>{cargando ? "Buscando…" : "Buscar cliente"}</Button>
      <Button variant="outline" onClick={() => { solicitud.current?.abort(); setDocumento(""); setCargando(false); setError(""); onChange(null); }}>Usar Cliente varios</Button>
    </div>
    <p role="status" className="text-sm text-slate-600">{cliente ? `${cliente.nombre} · ${cliente.numeroDocumento}` : "Cliente varios seleccionado."}</p>
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    {documento && !cliente && <p className="text-sm text-amber-800">Busca y selecciona el cliente, o borra el documento para usar Cliente varios.</p>}
  </fieldset>;
}

export type { ClienteVenta };
