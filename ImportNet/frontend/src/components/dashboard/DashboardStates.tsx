import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";

export function DashboardLoading({ graficos = false }: { graficos?: boolean }) {
  return <div role="status" aria-label="Cargando dashboard" className={graficos ? "grid gap-4 lg:grid-cols-2" : "grid gap-4 sm:grid-cols-2 xl:grid-cols-4"}>
    <span className="sr-only">Cargando datos del dashboard…</span>
    {Array.from({ length: graficos ? 3 : 4 }, (_, i) => <div key={i} className={`rounded-xl border p-6 ${graficos && i === 0 ? "lg:col-span-2" : ""}`} aria-hidden="true">
      <Skeleton className="h-4 w-28" /><Skeleton className={`mt-5 w-full ${graficos ? "h-52" : "h-8"}`} />
    </div>)}
  </div>;
}

export function DashboardError({ mensaje, reintentar }: { mensaje: string; reintentar: () => void }) {
  return <div role="alert" className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-red-200 bg-red-50 p-4">
    <p className="text-sm text-red-800">{mensaje}</p><Button variant="outline" onClick={reintentar}>Reintentar</Button>
  </div>;
}
