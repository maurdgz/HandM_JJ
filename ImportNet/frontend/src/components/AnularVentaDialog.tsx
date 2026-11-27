import { useEffect, useState, type SubmitEvent } from "react";
import { toast } from "sonner";

import { api } from "@/lib/api";
import { pen } from "@/lib/compras";
import { MOTIVO_MAXIMO, numeroVenta, type VentaAnulada } from "@/lib/ventas";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";

export interface VentaAAnular {
  id: number;
  numero: number;
  total: number;
}

interface Props {
  venta: VentaAAnular | null;
  onCerrar: () => void;
  onAnulada: () => void;
}

export default function AnularVentaDialog({ venta, onCerrar, onAnulada }: Props) {
  const [motivo, setMotivo] = useState("");
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!venta) return;
    setMotivo("");
    setError("");
  }, [venta]);

  async function anular(event: SubmitEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (!venta || guardando) return;

    setGuardando(true);
    setError("");

    try {
      const resultado = await api<VentaAnulada>(`/ventas/${venta.id}/anulacion`, {
        method: "POST",
        body: JSON.stringify({ motivo: motivo.trim() }),
      });

      toast.success(
        `${numeroVenta(resultado.numero)} anulada. Stock repuesto en ${resultado.reposiciones.length} producto(s).`,
      );
      onAnulada();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo anular la venta.");
    } finally {
      setGuardando(false);
    }
  }

  return (
    <Dialog open={venta !== null} onOpenChange={(open) => { if (!open && !guardando) onCerrar(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Anular venta</DialogTitle>
          <DialogDescription>
            {venta && `${numeroVenta(venta.numero)} por ${pen(venta.total)}. `}
            El stock de cada línea se repone una sola vez. Esta acción no se puede deshacer.
          </DialogDescription>
        </DialogHeader>

        <form id="form-anulacion" onSubmit={anular} className="space-y-4">
          {error && <p role="alert" className="rounded bg-red-50 p-2 text-red-700">{error}</p>}

          <div className="space-y-2">
            <Label htmlFor="anulacion-motivo">Motivo</Label>
            <textarea
              id="anulacion-motivo"
              required
              maxLength={MOTIVO_MAXIMO}
              rows={3}
              className="w-full rounded border p-2"
              placeholder="Por ejemplo: cliente devolvió el producto"
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
            <p className="text-right text-sm text-slate-600">
              {motivo.length}/{MOTIVO_MAXIMO}
            </p>
          </div>
        </form>

        <DialogFooter>
          <Button type="button" variant="outline" disabled={guardando} onClick={onCerrar}>
            Cancelar
          </Button>
          <Button
            type="submit"
            form="form-anulacion"
            variant="destructive"
            disabled={guardando || motivo.trim() === ""}
          >
            {guardando ? "Anulando…" : "Anular venta"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
