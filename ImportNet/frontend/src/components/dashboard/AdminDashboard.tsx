import { useCallback, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { obtenerGraficos, obtenerResumen, periodoMeses } from "@/lib/dashboard";
import IndicadoresDashboard from "./IndicadoresDashboard";
import VentasMesChart from "./VentasMesChart";
import TopProductosChart from "./TopProductosChart";
import VentasVendedorChart from "./VentasVendedorChart";
import { DashboardError, DashboardLoading } from "./DashboardStates";
import { useDashboardResource } from "./useDashboardResource";

export default function AdminDashboard() {
  const [meses, setMeses] = useState("6");
  const [revision, setRevision] = useState(0);
  const resumen = useDashboardResource(obtenerResumen, revision);
  const periodo = useMemo(() => periodoMeses(Number(meses), resumen.datos?.fecha), [meses, resumen.datos?.fecha]);
  const cargarGraficos = useCallback((signal: AbortSignal) => obtenerGraficos(periodo, signal), [periodo]);
  const graficos = useDashboardResource(cargarGraficos, revision);
  const actualizar = () => setRevision(r => r + 1);
  return <section className="space-y-6">
    <header className="flex flex-wrap items-start justify-between gap-4">
      <div><h1 className="text-2xl font-semibold">Dashboard</h1><p className="mt-1 text-sm text-slate-600">Ventas e inventario de ImportNet · PEN · Hora de Perú</p></div>
      <Button variant="outline" onClick={actualizar} disabled={resumen.cargando || graficos.cargando}><RefreshCw className="size-4" aria-hidden="true" />Actualizar</Button>
    </header>
    {resumen.cargando && <DashboardLoading />}
    {resumen.error && <DashboardError mensaje={resumen.error} reintentar={actualizar} />}
    {resumen.datos && <>
      <p className="text-xs text-slate-500">Indicadores del {resumen.datos.fecha} · Mes calendario hasta hoy · Inventario actual</p>
      <IndicadoresDashboard datos={resumen.datos} />
    </>}
    <div className="flex flex-wrap items-end justify-between gap-4 border-t pt-6">
      <div><h2 className="text-lg font-semibold">Evolución comercial</h2><p className="mt-1 text-sm text-slate-500">{periodo.desde} al {periodo.hasta} · Las ventas anuladas se excluyen.</p></div>
      <div className="space-y-2"><label htmlFor="periodo-dashboard" className="block text-sm font-medium">Periodo de los gráficos</label>
        <Select value={meses} onValueChange={setMeses}><SelectTrigger id="periodo-dashboard" className="w-52"><SelectValue /></SelectTrigger>
          <SelectContent><SelectItem value="1">Este mes</SelectItem><SelectItem value="3">Últimos 3 meses</SelectItem><SelectItem value="6">Últimos 6 meses</SelectItem><SelectItem value="12">Últimos 12 meses</SelectItem></SelectContent>
        </Select>
      </div>
    </div>
    {graficos.cargando && <DashboardLoading graficos />}
    {graficos.error && <DashboardError mensaje={graficos.error} reintentar={actualizar} />}
    {graficos.datos && <div className="grid gap-4 lg:grid-cols-2">
      <div className="min-w-0 lg:col-span-2"><VentasMesChart datos={graficos.datos.meses} /></div>
      <TopProductosChart datos={graficos.datos.productos} /><VentasVendedorChart datos={graficos.datos.vendedores} />
    </div>}
  </section>;
}
