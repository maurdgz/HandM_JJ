import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { cantidad, pen, type VentasVendedor } from "@/lib/dashboard";

export default function VentasVendedorChart({ datos }: { datos: VentasVendedor[] }) {
  return <Card className="min-w-0 shadow-none">
    <CardHeader><CardTitle>Ventas por vendedor</CardTitle><CardDescription>Importes netos en PEN · Periodo seleccionado</CardDescription></CardHeader>
    <CardContent>{datos.length ? <>
      <div className="max-h-96 overflow-y-auto">
        <ChartContainer config={{ total: { label: "Ventas netas (PEN)", color: "hsl(215 65% 38%)" } }} className="w-full aspect-auto" style={{ height: Math.max(240, datos.length * 42) }} aria-label="Ventas por vendedor en PEN">
          <BarChart accessibilityLayer data={datos} layout="vertical" margin={{ right: 16, left: 0 }}>
            <CartesianGrid horizontal={false} /><XAxis type="number" tickFormatter={v => cantidad(Number(v))} />
            <YAxis type="category" dataKey="vendedor" width={100} tickFormatter={v => String(v).length > 14 ? `${String(v).slice(0, 14)}…` : String(v)} tickLine={false} axisLine={false} />
            <ChartTooltip content={<ChartTooltipContent formatter={v => pen(Number(v))} />} />
            <Bar dataKey="total" fill="var(--color-total)" radius={[0, 4, 4, 0]} maxBarSize={30} isAnimationActive={false} />
          </BarChart>
        </ChartContainer>
      </div>
      <details className="mt-4 text-sm"><summary className="cursor-pointer text-slate-600">Ver ventas y ticket promedio</summary>
        <div className="overflow-x-auto"><table className="mt-3 w-full text-left"><caption className="sr-only">Ventas por vendedor, cantidades, totales y ticket promedio PEN</caption>
          <thead><tr><th scope="col">Vendedor</th><th scope="col">Ventas</th><th scope="col">Total PEN</th><th scope="col">Ticket PEN</th></tr></thead>
          <tbody>{datos.map(d => <tr className="border-t" key={d.empleadoId}><th scope="row" className="py-2 pr-3 font-normal">{d.vendedor}</th><td>{cantidad(d.ventas)}</td><td className="whitespace-nowrap pr-3">{pen(d.total)}</td><td className="whitespace-nowrap">{pen(d.ticketPromedio)}</td></tr>)}</tbody>
        </table></div>
      </details>
    </> : <p className="flex min-h-48 items-center justify-center text-center text-sm text-slate-500">No hay ventas por vendedor en este periodo.</p>}</CardContent>
  </Card>;
}
