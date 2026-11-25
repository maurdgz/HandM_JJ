import { useEffect, useState } from "react";
import { toast } from "sonner";
import { api, ApiError } from "@/lib/api";
import { fechaHora, fechaPeru, moneda } from "@/lib/format";
import type { UsuarioDto, VentaResumen } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default function VentasPage({ user }: { user: UsuarioDto }) {
  const [desde, setDesde] = useState(() => fechaPeru(30));
  const [hasta, setHasta] = useState(() => fechaPeru());
  const [periodo, setPeriodo] = useState(() => ({ desde: fechaPeru(30), hasta: fechaPeru() }));
  const [rows, setRows] = useState<VentaResumen[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<VentaResumen | null>(null);
  const [motivo, setMotivo] = useState("");
  const [pending, setPending] = useState(false);
  const isAdmin = user.roles.includes("Administrador");

  async function load(start: string, end: string, signal?: AbortSignal) {
    setLoading(true);
    setError("");
    try {
      const query = new URLSearchParams({ desde: start, hasta: end });
      const ventas = await api<VentaResumen[]>(`/ventas?${query}`, { signal });
      if (signal?.aborted) return;
      setRows(ventas);
      setPeriodo({ desde: start, hasta: end });
    } catch (error) {
      if (!signal?.aborted) setError(error instanceof Error ? error.message : "No se pudo cargar el historial.");
    } finally {
      if (!signal?.aborted) setLoading(false);
    }
  }

  useEffect(() => {
    const controller = new AbortController();
    void load(fechaPeru(30), fechaPeru(), controller.signal);
    return () => controller.abort();
  }, []);

  async function anular() {
    if (!selected || !motivo.trim() || pending) return;
    setPending(true);
    try {
      await api<void>(`/ventas/${selected.id}/anulacion`, {
        method: "POST", body: JSON.stringify({ motivo: motivo.trim() }),
      });
      setSelected(null);
      setMotivo("");
      toast.success("Venta anulada y stock repuesto.");
      await load(periodo.desde, periodo.hasta);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No se pudo anular la venta.");
      if (error instanceof ApiError && error.status === 409) {
        setSelected(null);
        await load(periodo.desde, periodo.hasta);
      }
    } finally {
      setPending(false);
    }
  }

  return <section className="space-y-6">
    <header>
      <h1 className="text-2xl font-semibold">Historial de ventas</h1>
      <p className="mt-2 text-sm text-slate-600">
        {isAdmin ? "Ventas de todos los vendedores." : "Tus ventas registradas."} Las fechas corresponden a Perú.
      </p>
    </header>

    <form className="flex flex-wrap items-end gap-4" onSubmit={event => {
      event.preventDefault();
      if (!loading && !pending) void load(desde, hasta);
    }}>
      <div className="space-y-2"><Label htmlFor="ventas-desde">Desde</Label>
        <Input id="ventas-desde" type="date" required disabled={loading || pending} value={desde} onChange={e => setDesde(e.target.value)} />
      </div>
      <div className="space-y-2"><Label htmlFor="ventas-hasta">Hasta</Label>
        <Input id="ventas-hasta" type="date" required min={desde} disabled={loading || pending} value={hasta} onChange={e => setHasta(e.target.value)} />
      </div>
      <Button type="submit" disabled={loading || pending}>{loading ? "Consultando…" : "Filtrar"}</Button>
    </form>

    {error && <div role="alert" className="space-y-3">
      <p className="text-red-700">{error}</p>
      <Button variant="outline" disabled={loading || pending} onClick={() => void load(desde, hasta)}>Reintentar</Button>
    </div>}

    {loading ? <p role="status">Cargando ventas…</p> : error ? null : rows.length === 0 ?
      <p role="status">No hay ventas en el periodo seleccionado.</p> :
      <div className="overflow-x-auto rounded-lg border"><Table>
        <TableHeader><TableRow>
          <TableHead>Número</TableHead><TableHead>Fecha</TableHead><TableHead>Vendedor</TableHead>
          <TableHead>Cliente</TableHead><TableHead>Estado</TableHead><TableHead>Total</TableHead><TableHead>Acciones</TableHead>
        </TableRow></TableHeader>
        <TableBody>{rows.map(venta => <TableRow key={venta.id}>
          <TableCell>{venta.numero}</TableCell><TableCell>{fechaHora(venta.fecha)}</TableCell>
          <TableCell>{venta.vendedor}</TableCell><TableCell>{venta.cliente}</TableCell>
          <TableCell>{venta.estado}</TableCell><TableCell>{moneda(venta.total)}</TableCell>
          <TableCell><div className="flex gap-2">
            <Button variant="outline" asChild><a href={`/ventas/nota?id=${venta.id}`}>Ver nota</a></Button>
            {isAdmin && venta.estado === "Registrada" && <Button variant="destructive" disabled={pending} onClick={() => {
              setMotivo(""); setSelected(venta);
            }}>Anular</Button>}
          </div></TableCell>
        </TableRow>)}</TableBody>
      </Table></div>}

    <Dialog open={selected !== null} onOpenChange={open => {
      if (!open && !pending) { setSelected(null); setMotivo(""); }
    }}>
      <DialogContent onEscapeKeyDown={event => { if (pending) event.preventDefault(); }} onInteractOutside={event => { if (pending) event.preventDefault(); }}>
        <DialogHeader>
          <DialogTitle>Anular venta {selected?.numero}</DialogTitle>
          <DialogDescription>La venta quedará anulada y sus cantidades volverán al inventario.</DialogDescription>
        </DialogHeader>
        <form className="space-y-5" onSubmit={event => { event.preventDefault(); void anular(); }}>
          <div className="space-y-2"><Label htmlFor="motivo-anulacion">Motivo de anulación</Label>
            <textarea id="motivo-anulacion" required maxLength={300} rows={4} disabled={pending}
              className="w-full rounded-md border p-3 text-sm" value={motivo} onChange={e => setMotivo(e.target.value)} />
            <p className="text-xs text-slate-500">Máximo 300 caracteres.</p>
          </div>
          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" disabled={pending} onClick={() => setSelected(null)}>Cancelar</Button>
            <Button type="submit" variant="destructive" disabled={pending || !motivo.trim()}>{pending ? "Anulando…" : "Confirmar anulación"}</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  </section>;
}
