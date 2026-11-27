import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cantidad, obtenerMiResumen, pen } from "@/lib/dashboard";
import { DashboardError, DashboardLoading } from "./DashboardStates";
import { useDashboardResource } from "./useDashboardResource";

export default function VendedorDashboard() {
  const [revision, setRevision] = useState(0);
  const resumen = useDashboardResource(obtenerMiResumen, revision);
  const actualizar = () => setRevision(r => r + 1);
  return <section className="space-y-6">
    <header className="flex flex-wrap items-start justify-between gap-4"><div>
      <h1 className="text-2xl font-semibold">Mis ventas de hoy</h1><p className="mt-1 text-sm text-slate-600">Tu actividad del día · PEN · Hora de Perú</p>
    </div><Button variant="outline" onClick={actualizar} disabled={resumen.cargando}>Actualizar</Button></header>
    {resumen.cargando && <DashboardLoading />}
    {resumen.error && <DashboardError mensaje={resumen.error} reintentar={actualizar} />}
    {resumen.datos && <>
      <p className="text-xs text-slate-500">Fecha comercial: {resumen.datos.fecha} · Solo tus ventas registradas</p>
      <div className="grid gap-4 sm:grid-cols-3">
        {[
          ["Total vendido hoy", pen(resumen.datos.total)],
          ["Ventas realizadas", cantidad(resumen.datos.ventas)],
          ["Ticket promedio", pen(resumen.datos.ventas ? resumen.datos.total / resumen.datos.ventas : 0)],
        ].map(([titulo, valor]) => <Card className="shadow-none" key={titulo}><CardHeader><CardTitle className="text-sm font-medium text-slate-600">{titulo}</CardTitle></CardHeader><CardContent><p className="break-words text-2xl font-semibold tabular-nums">{valor}</p></CardContent></Card>)}
      </div>
      {resumen.datos.ventas === 0 && <p role="status" className="rounded-lg border bg-slate-50 p-6 text-sm text-slate-600">Todavía no has registrado ventas hoy.</p>}
      {resumen.datos.anuladas > 0 && <p className="text-sm text-slate-600">{cantidad(resumen.datos.anuladas)} ventas anuladas ({pen(resumen.datos.montoAnulado)}) excluidas del total.</p>}
      <div className="flex flex-wrap gap-3"><Button asChild><a href="/ventas">Ir al punto de venta</a></Button><Button asChild variant="outline"><a href="/historial">Ver mis ventas</a></Button></div>
    </>}
  </section>;
}
