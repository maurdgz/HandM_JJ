export type Rol = "Administrador" | "Vendedor";

export interface TokenDto {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
}

export interface UsuarioDto {
  id: string;
  email: string;
  empleadoId: number;
  nombre: string;
  apellidos: string;
  roles: Rol[];
}

export interface EmpleadoDatos {
  nombre: string;
  apellidos: string;
  documentoIdentidad: string;
  email: string;
  telefono: string;
  cargo: string;
  area: string;
}

export interface EmpleadoDto {
  id: number;
  usuarioId: string;
  nombre: string;
  apellidos: string;
  documentoIdentidad: string | null;
  email: string;
  telefono: string;
  cargo: string;
  area: string;
  activo: boolean;
  roles: Rol[];
}

export interface VentaResumen {
  id: number;
  numero: number;
  fecha: string;
  empleadoId: number;
  vendedor: string;
  cliente: string;
  documentoCliente: string | null;
  estado: "Registrada" | "Anulada";
  subtotal: number;
  descuento: number;
  total: number;
  motivoAnulacion: string | null;
}

export interface VentaLinea {
  codigo: string;
  producto: string;
  unidadMedida: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  subtotal: number;
}

export interface NotaVenta {
  venta: VentaResumen;
  lineas: VentaLinea[];
}
