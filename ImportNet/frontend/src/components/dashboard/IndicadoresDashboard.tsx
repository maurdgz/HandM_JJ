import { Banknote, CalendarDays, PackageCheck, TriangleAlert, TrendingUp } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cantidad, pen, type ResumenDashboard } from "@/lib/dashboard";

export default function IndicadoresDashboard({ datos }: { datos: ResumenDashboard }) {
  const tarjetas = [
    { titulo: "Ventas de hoy", valor: pen(datos.ventasHoy), detalle: `${cantidad(datos.cantidadVentasHoy)} ventas registradas`, icono: Banknote },
    { titulo: "Ventas del mes", valor: pen(datos.ventasMes), detalle: `${cantidad(datos.cantidadVentasMes)} ventas · Ticket ${pen(datos.ticketPromedioMes)}`, icono: CalendarDays },
    { titulo: "Margen del mes", valor: pen(datos.margenMes), detalle: "Calculado al costo promedio vigente", icono: TrendingUp },
    { titulo: "Productos bajo mínimo", valor: cantidad(datos.productosBajoMinimo), detalle: "Stock menor o igual al mínimo", icono: TriangleAlert },
  ];
  return <>
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {tarjetas.map(({ titulo, valor, detalle, icono: Icono }) => <Card key={titulo} className="min-w-0 gap-3 shadow-none">
        <CardHeader className="flex flex-row items-center justify-between gap-2">
          <CardTitle className="text-sm font-medium text-slate-600">{titulo}</CardTitle>
          <Icono aria-hidden="true" className="size-4 shrink-0 text-slate-500" />
        </CardHeader>
        <CardContent><p className="break-words text-2xl font-semibold tabular-nums" data-kpi={titulo}>{valor}</p>
          <p className="mt-2 text-xs leading-relaxed text-slate-500">{detalle}</p></CardContent>
      </Card>)}
    </div>
    <div className="flex flex-wrap items-center gap-x-6 gap-y-2 rounded-lg border bg-slate-50 px-4 py-3 text-sm">
      <span className="flex items-center gap-2 font-medium"><PackageCheck className="size-4" aria-hidden="true" />Inventario actual</span>
      <span>{cantidad(datos.productosActivos)} productos activos</span>
      <span>{cantidad(datos.productosSinStock)} sin stock</span>
      <a href="/stock" className="font-medium underline underline-offset-4">Ver stock y alertas</a>
    </div>
  </>;
}
