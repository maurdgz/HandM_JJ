import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { cantidad, pen, type VentasPeriodo } from "@/lib/dashboard";

const config = { total: { label: "Ventas netas (PEN)", color: "hsl(215 65% 38%)" } };
const mes = (valor: string) => new Intl.DateTimeFormat("es-PE", { month: "short", year: "2-digit", timeZone: "UTC" }).format(new Date(`${valor}-01T00:00:00Z`));

export default function VentasMesChart({ datos }: { datos: VentasPeriodo[] }) {
  const hayVentas = datos.some(d => d.ventas > 0);
  return <Card className="min-w-0 shadow-none">
    <CardHeader><CardTitle>Ventas por mes</CardTitle><CardDescription>Importes netos en PEN · Solo ventas registradas</CardDescription></CardHeader>
    <CardContent>
      {hayVentas ? <>
        <ChartContainer config={config} className="h-64 w-full aspect-auto" aria-label="Evolución mensual de ventas en PEN">
          <AreaChart accessibilityLayer data={datos} margin={{ left: 8, right: 12, top: 8, bottom: 8 }}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="periodo" tickFormatter={mes} tickLine={false} axisLine={false} minTickGap={20} />
            <YAxis width={64} tickLine={false} axisLine={false} tickFormatter={v => cantidad(Number(v))} label={{ value: "PEN", position: "insideTopLeft", offset: -2 }} />
            <ChartTooltip content={<ChartTooltipContent formatter={v => pen(Number(v))} labelFormatter={v => mes(String(v))} />} />
            <Area dataKey="total" type="linear" stroke="var(--color-total)" fill="var(--color-total)" fillOpacity={0.12} strokeWidth={2} dot={{ r: 3, fill: "var(--color-total)" }} isAnimationActive={false} />
          </AreaChart>
        </ChartContainer>
        <details className="mt-4 text-sm"><summary className="cursor-pointer text-slate-600">Ver datos por mes</summary>
          <div className="overflow-x-auto"><table className="mt-3 w-full text-left"><caption className="sr-only">Ventas por mes, cantidad e importe neto PEN</caption>
            <thead><tr><th scope="col">Mes</th><th scope="col">Ventas</th><th scope="col">Total PEN</th></tr></thead>
            <tbody>{datos.map(d => <tr key={d.periodo} className="border-t"><th scope="row" className="py-2 font-normal">{mes(d.periodo)}</th><td>{cantidad(d.ventas)}</td><td>{pen(d.total)}</td></tr>)}</tbody></table></div>
        </details>
      </> : <p className="flex min-h-48 items-center justify-center text-center text-sm text-slate-500">No hay ventas registradas en este periodo.</p>}
    </CardContent>
  </Card>;
}
