import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { cantidad, pen, type TopProducto } from "@/lib/dashboard";

export default function TopProductosChart({ datos }: { datos: TopProducto[] }) {
  return <Card className="min-w-0 shadow-none">
    <CardHeader><CardTitle>Productos más vendidos</CardTitle><CardDescription>Top 5 por cantidad · Unidad de medida de cada producto</CardDescription></CardHeader>
    <CardContent>{datos.length ? <>
      <ChartContainer config={{ cantidad: { label: "Cantidad vendida", color: "hsl(175 48% 32%)" } }} className="h-64 w-full aspect-auto" aria-label="Productos más vendidos por cantidad">
        <BarChart accessibilityLayer data={datos} layout="vertical" margin={{ right: 16, left: 0 }}>
          <CartesianGrid horizontal={false} /><XAxis type="number" tickFormatter={v => cantidad(Number(v))} />
          <YAxis type="category" dataKey="codigo" width={90} tickFormatter={v => String(v).slice(0, 13)} tickLine={false} axisLine={false} />
          <ChartTooltip content={<ChartTooltipContent formatter={v => cantidad(Number(v))} labelFormatter={(_, payload) => payload[0]?.payload?.nombre ?? "Producto"} />} />
          <Bar dataKey="cantidad" fill="var(--color-cantidad)" radius={[0, 4, 4, 0]} maxBarSize={30} isAnimationActive={false} />
        </BarChart>
      </ChartContainer>
      <details className="mt-4 text-sm"><summary className="cursor-pointer text-slate-600">Ver productos e importes</summary>
        <div className="overflow-x-auto"><table className="mt-3 w-full text-left"><caption className="sr-only">Ranking por cantidad vendida y ventas netas en PEN</caption>
          <thead><tr><th scope="col">Producto</th><th scope="col">Cantidad</th><th scope="col">Total PEN</th></tr></thead>
          <tbody>{datos.map(d => <tr className="border-t" key={d.productoId}><th scope="row" className="py-2 pr-3 font-normal">{d.nombre}<span className="block text-xs text-slate-500">{d.codigo}</span></th><td className="pr-3">{cantidad(d.cantidad)} {d.unidadMedida}</td><td className="whitespace-nowrap">{pen(d.total)}</td></tr>)}</tbody>
        </table></div>
      </details>
    </> : <p className="flex min-h-48 items-center justify-center text-center text-sm text-slate-500">No hay productos vendidos en este periodo.</p>}</CardContent>
  </Card>;
}
