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