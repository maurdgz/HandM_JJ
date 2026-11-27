import { api } from "@/lib/api";

export interface ResumenDashboard {
  fecha: string;
  ventasHoy: number;
  cantidadVentasHoy: number;
  ventasMes: number;
  cantidadVentasMes: number;
  ticketPromedioMes: number;
  margenMes: number;
  productosActivos: number;
  productosBajoMinimo: number;
  productosSinStock: number;
}
export interface VentasPeriodo { periodo: string; ventas: number; total: number }
export interface TopProducto { productoId: number; codigo: string; nombre: string; unidadMedida: string; cantidad: number; total: number }
export interface VentasVendedor { empleadoId: number; vendedor: string; ventas: number; total: number; ticketPromedio: number }
export interface MiResumen {
  fecha: string; ventas: number; subtotal: number; descuento: number; total: number;
  anuladas: number; montoAnulado: number;
}
export interface GraficosDashboard {
  meses: VentasPeriodo[];
  productos: TopProducto[];
  vendedores: VentasVendedor[];
}
export interface PeriodoDashboard { desde: string; hasta: string }

export const pen = (valor: number) => new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(valor);
export const cantidad = (valor: number) => new Intl.NumberFormat("es-PE", { maximumFractionDigits: 3 }).format(valor);
export const fechaPeru = () => new Intl.DateTimeFormat("en-CA", {
  timeZone: "America/Lima", year: "numeric", month: "2-digit", day: "2-digit",
}).format(new Date());

/** Meses calendario incluyendo el actual, con fin en el día comercial de Perú. */
export function periodoMeses(meses: number, hoy = fechaPeru()): PeriodoDashboard {
  const [ano, mes] = hoy.split("-").map(Number);
  const inicio = new Date(Date.UTC(ano, mes - meses, 1));
  return { desde: inicio.toISOString().slice(0, 10), hasta: hoy };
}

export const obtenerResumen = (signal: AbortSignal) => api<ResumenDashboard>("/reportes/dashboard", { signal });
export const obtenerMiResumen = (signal: AbortSignal) => api<MiResumen>("/reportes/mi-resumen", { signal });

export async function obtenerGraficos(periodo: PeriodoDashboard, signal: AbortSignal): Promise<GraficosDashboard> {
  const parametros = new URLSearchParams(periodo).toString();
  const [meses, productos, vendedores] = await Promise.all([
    api<VentasPeriodo[]>(`/reportes/ventas-por-periodo?${parametros}&agrupacion=Mes`, { signal }),
    api<TopProducto[]>(`/reportes/top-productos?${parametros}&top=5`, { signal }),
    api<VentasVendedor[]>(`/reportes/ventas-por-vendedor?${parametros}`, { signal }),
  ]);
  // Un periodo sin filas del reporte representa cero ventas, no una proyección.
  const porMes = new Map(meses.map(m => [m.periodo, m]));
  const completos: VentasPeriodo[] = [];
  let cursor = new Date(`${periodo.desde}T00:00:00Z`);
  const ultimo = periodo.hasta.slice(0, 7);
  while (cursor.toISOString().slice(0, 7) <= ultimo) {
    const clave = cursor.toISOString().slice(0, 7);
    completos.push(porMes.get(clave) ?? { periodo: clave, ventas: 0, total: 0 });
    cursor = new Date(Date.UTC(cursor.getUTCFullYear(), cursor.getUTCMonth() + 1, 1));
  }
  return { meses: completos, productos, vendedores };
}
