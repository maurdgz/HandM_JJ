export interface LineaCompra { productoId: number; cantidad: number; costoUnitarioUsd: number }
export interface CompraDatos { proveedorId: number; fecha: string; tipoCambio: number; fletePen: number; aduanaPen: number; lineas: LineaCompra[] }
export interface CostoLinea extends LineaCompra { subtotalUsd: number; mercaderiaPen: number; fletePen: number; aduanaPen: number; totalPen: number; costoUnitarioPuestoPen: number }
export interface Compra extends CompraDatos { id: number; estado: "Borrador" | "Recibida"; subtotalUsd: number; mercaderiaPen: number; totalPen: number; lineas: CostoLinea[] }
export interface Pagina<T> { items: T[]; count: number; paginaActual: number; tamano: number }
export const pen = (n: number) => new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(n);
export const usd = (n: number) => new Intl.NumberFormat("es-PE", { style: "currency", currency: "USD" }).format(n);
