import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import type { AnalisisIaDto } from "@/lib/types";

interface Props {
  desde?: string;
  hasta?: string;
}

export default function AsistenteIaCard({ desde, hasta }: Props) {
  const [pregunta, setPregunta] = useState("");
  const [analisis, setAnalisis] = useState<AnalisisIaDto | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState("");

  async function solicitarAnalisis(evento?: FormEvent<HTMLFormElement>) {
    evento?.preventDefault();
    setCargando(true);
    setError("");

    try {
      const resultado = await api<AnalisisIaDto>("/ia/analizar", {
        method: "POST",
        body: JSON.stringify({
          desde: desde || null,
          hasta: hasta || null,
          pregunta: pregunta.trim() || null,
        }),
      });
      setAnalisis(resultado);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "No se pudo generar el análisis de IA.");
    } finally {
      setCargando(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Asistente IA de Negocio</CardTitle>
        <CardDescription>
          Analiza KPIs, márgenes y alertas de reposición del periodo ({desde ?? "mes actual"} al {hasta ?? "hoy"}) usando el proveedor de IA externo configurado.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <form onSubmit={(e) => void solicitarAnalisis(e)} className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1 space-y-2">
            <Label htmlFor="pregunta-ia">Pregunta opcional para la IA</Label>
            <Input
              id="pregunta-ia"
              type="text"
              maxLength={500}
              placeholder="Ej. ¿Qué productos debo reponer primero y dónde tengo mayor margen?"
              value={pregunta}
              onChange={(e) => setPregunta(e.target.value)}
              disabled={cargando}
            />
          </div>
          <Button type="submit" disabled={cargando}>
            {cargando ? "Analizando…" : "Generar análisis IA"}
          </Button>
        </form>

        {error && (
          <p role="alert" className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </p>
        )}

        {cargando && (
          <div className="space-y-2" aria-busy="true">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-2/3" />
          </div>
        )}

        {analisis && !cargando && (
          <div className="space-y-4 rounded-lg border bg-slate-50/70 p-4 text-sm">
            <div className="space-y-1">
              <h3 className="font-semibold text-slate-900">Resumen ejecutivo</h3>
              <p className="text-slate-700">{analisis.resumenEjecutivo}</p>
            </div>

            {analisis.respuestaPregunta && (
              <div className="space-y-1 rounded-md border bg-white p-3">
                <h4 className="font-semibold text-slate-900">Respuesta a tu consulta</h4>
                <p className="text-slate-700">{analisis.respuestaPregunta}</p>
              </div>
            )}

            {analisis.alertasPrioritarias.length > 0 && (
              <div className="space-y-1">
                <h4 className="font-semibold text-amber-800">Alertas prioritarias</h4>
                <ul className="list-disc space-y-1 pl-5 text-slate-700">
                  {analisis.alertasPrioritarias.map((alerta, index) => (
                    <li key={index}>{alerta}</li>
                  ))}
                </ul>
              </div>
            )}

            {analisis.recomendaciones.length > 0 && (
              <div className="space-y-1">
                <h4 className="font-semibold text-slate-900">Recomendaciones</h4>
                <ul className="list-disc space-y-1 pl-5 text-slate-700">
                  {analisis.recomendaciones.map((rec, index) => (
                    <li key={index}>{rec}</li>
                  ))}
                </ul>
              </div>
            )}

            <p className="text-xs text-slate-500">
              Proveedor: {analisis.proveedorIa} ({analisis.modelo}) · Periodo {analisis.desde} al {analisis.hasta}
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
