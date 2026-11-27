import { useEffect, useState } from "react";

/** Cancela solicitudes obsoletas y evita mostrar datos de un periodo anterior. */
export function useDashboardResource<T>(cargar: (signal: AbortSignal) => Promise<T>, revision: number) {
  const [estado, setEstado] = useState<{ datos?: T; error?: string; cargando: boolean }>({ cargando: true });
  useEffect(() => {
    const controller = new AbortController();
    setEstado({ cargando: true });
    cargar(controller.signal).then(datos => {
      if (!controller.signal.aborted) setEstado({ datos, cargando: false });
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setEstado({ cargando: false,
        error: error instanceof Error ? error.message : "No se pudieron cargar los datos." });
    });
    return () => controller.abort();
  }, [cargar, revision]);
  return estado;
}
