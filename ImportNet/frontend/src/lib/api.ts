import { clearSession, getToken } from "@/lib/session";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

interface ApiOptions extends RequestInit {
  auth?: boolean;
}

function redirectToLogin(): void {
  clearSession();
  window.location.replace("/login");
}

function errorMessage(data: unknown, status: number): string {
  if (typeof data === "object" && data !== null) {
    const problem = data as {
      title?: string;
      detail?: string;
      errors?: Record<string, string[]>;
    };

    if (problem.errors) {
      const messages = Object.values(problem.errors).flat();

      if (messages.length > 0) {
        return messages.join(" ");
      }
    }

    if (problem.detail) return problem.detail;
    if (problem.title) return problem.title;
  }

  if (status === 403) return "No tienes permiso para realizar esta acción.";
  if (status === 404) return "No se encontró el registro.";
  if (status === 409) return "Hay un conflicto. Recarga los datos.";
  if (status === 401) return "La sesión no es válida.";

  return "No se pudo completar la solicitud.";
}

async function solicitar(path: string, options: ApiOptions, accept: string): Promise<Response> {
  const { auth = true, ...request } = options;
  const headers = new Headers(request.headers);

  headers.set("Accept", accept);

  if (request.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (auth) {
    const token = getToken();

    if (!token) {
      redirectToLogin();
      throw new ApiError(401, "Debes iniciar sesión.");
    }

    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`/api${path}`, {
    ...request,
    headers,
    cache: "no-store",
  });

  if (response.ok) return response;

  if (response.status === 401 && auth) {
    redirectToLogin();
  }

  throw new ApiError(response.status, errorMessage(await leerJson(response), response.status));
}

async function leerJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

export async function api<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const response = await solicitar(path, options, "application/json");
  return await leerJson(response) as T;
}

export async function apiExcel(path: string): Promise<Blob> {
  const tipo = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
  const response = await solicitar(path, {}, tipo);
  if (response.headers.get("Content-Type")?.split(";")[0] !== tipo) {
    throw new ApiError(response.status, "La API no devolvió un archivo Excel.");
  }
  return response.blob();
}
