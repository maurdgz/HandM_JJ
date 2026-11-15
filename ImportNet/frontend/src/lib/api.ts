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

export async function api<T>(
  path: string,
  options: ApiOptions = {},
): Promise<T> {
  const { auth = true, ...request } = options;
  const headers = new Headers(request.headers);

  headers.set("Accept", "application/json");

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

  const text = await response.text();
  let data: unknown;

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = undefined;
    }
  }

  if (!response.ok) {
    if (response.status === 401 && auth) {
      redirectToLogin();
    }

    throw new ApiError(
      response.status,
      errorMessage(data, response.status),
    );
  }

  return data as T;
}