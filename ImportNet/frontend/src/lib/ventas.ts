export interface ResumenVenta {
  id: number;
  numero: number;
  fecha: string;
  estado: "Registrada" | "Anulada";
  empleadoId: number;
  vendedor: string;
  cliente: string;
  lineas: number;
  total: number;
}

export interface LineaNotaVenta {
  productoId: number;
  codigo: string;
  producto: string;
  unidadMedida: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  subtotal: number;
}

export interface NotaVenta {
  id: number;
  numero: number;
  fecha: string;
  estado: "Registrada" | "Anulada";
  empleadoId: number;
  vendedor: string;
  clienteTipoDocumento: string;
  clienteDocumento: string;
  cliente: string;
  subtotal: number;
  descuento: number;
  total: number;
  motivoAnulacion: string | null;
  fechaAnulacion: string | null;
  lineas: LineaNotaVenta[];
}

export interface VentaAnulada {
  id: number;
  numero: number;
  estado: string;
  motivo: string;
  fechaAnulacion: string;
  reposiciones: { productoId: number; cantidad: number; stockNuevo: number }[];
}

export const MOTIVO_MAXIMO = 300;

export const numeroVenta = (n: number) => `NV-${String(n).padStart(6, "0")}`;

export const fechaHora = new Intl.DateTimeFormat("es-PE", {
  dateStyle: "short",
  timeStyle: "short",
  timeZone: "America/Lima",
});

export const cantidad = new Intl.NumberFormat("es-PE", { maximumFractionDigits: 3 });
