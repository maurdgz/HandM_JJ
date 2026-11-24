import { useCallback, useEffect, useRef, useState } from "react";
import { api, ApiError } from "@/lib/api";
import {
  type Compra,
  type CompraDatos,
  type Pagina,
  pen,
  usd,
} from "@/lib/compras";
import { Button } from "@/components/ui/button";
import CompraForm from "@/components/CompraForm";
import ResumenCompra from "@/components/ResumenCompra";

function mensajeError(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "No se pudo completar la operación.";
}

export default function ComprasPage() {
  const [formulario, setFormulario] = useState(false);
  const [editando, setEditando] = useState<Compra | undefined>();
  const [detalle, setDetalle] = useState<Compra | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [mensaje, setMensaje] = useState("");
  const [error, setError] = useState("");
  const [pagina, setPagina] = useState(1);
  const [lista, setLista] = useState<Pagina<Compra>>({
    items: [],
    count: 0,
    paginaActual: 1,
    tamano: 20,
  });

  const operacionEnCurso = useRef(false);
  const ultimaCarga = useRef(0);
  const bloqueado = busy || loading;

  const cargar = useCallback(async (): Promise<boolean> => {
    const solicitud = ++ultimaCarga.current;
    setLoading(true);

    try {
      const resultado = await api<Pagina<Compra>>(
        `/compras?pagina=${pagina}&tamano=20`,
      );

      if (solicitud !== ultimaCarga.current) return false;

      setLista(resultado);
      return true;
    } catch (e) {
      if (solicitud === ultimaCarga.current) {
        setError(mensajeError(e));
      }

      return false;
    } finally {
      if (solicitud === ultimaCarga.current) {
        setLoading(false);
      }
    }
  }, [pagina]);

  useEffect(() => {
    void cargar();

    return () => {
      ultimaCarga.current++;
    };
  }, [cargar]);

  function iniciarOperacion(): boolean {
    if (operacionEnCurso.current) return false;

    operacionEnCurso.current = true;
    setBusy(true);
    setError("");
    setMensaje("");
    return true;
  }

  function finalizarOperacion(): void {
    operacionEnCurso.current = false;
    setBusy(false);
  }

  function nuevaCompra(): void {
    if (bloqueado || operacionEnCurso.current) return;

    setEditando(undefined);
    setDetalle(null);
    setFormulario(true);
    setError("");
    setMensaje("");
  }

  async function consultar(id: number): Promise<void> {
    if (!iniciarOperacion()) return;

    try {
      const compra = await api<Compra>(`/compras/${id}`);
      setDetalle(compra);
      setFormulario(false);
      setEditando(undefined);
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      finalizarOperacion();
    }
  }

  async function guardar(datos: CompraDatos): Promise<void> {
    if (!iniciarOperacion()) return;

    try {
      const guardada = await api<Compra>(
        `/compras${editando ? `/${editando.id}` : ""}`,
        {
          method: editando ? "PUT" : "POST",
          body: JSON.stringify(datos),
        },
      );

      setDetalle(guardada);
      setFormulario(false);
      setEditando(undefined);
      setMensaje(
        `Borrador #${guardada.id} guardado. Total confirmado: ${pen(
          guardada.totalPen,
        )}.`,
      );

      await cargar();
    } finally {
      finalizarOperacion();
    }
  }

  async function editar(id: number): Promise<void> {
    if (!iniciarOperacion()) return;

    try {
      const compra = await api<Compra>(`/compras/${id}`);

      if (compra.estado !== "Borrador") {
        setDetalle(compra);
        setFormulario(false);
        setEditando(undefined);
        setError("Esta compra ya fue recibida y solo permite consulta.");
        await cargar();
        return;
      }

      setEditando(compra);
      setFormulario(true);
      setDetalle(null);
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      finalizarOperacion();
    }
  }

  async function eliminar(id: number): Promise<void> {
    if (operacionEnCurso.current) return;
    if (!window.confirm(`¿Eliminar el borrador #${id}?`)) return;
    if (!iniciarOperacion()) return;

    try {
      await api(`/compras/${id}`, { method: "DELETE" });

      setDetalle(null);
      setMensaje(`Borrador #${id} eliminado.`);

      if (lista.items.length === 1 && pagina > 1) {
        setPagina((actual) => actual - 1);
      } else {
        await cargar();
      }
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        setDetalle(null);
        await cargar();
      }

      setError(mensajeError(e));
    } finally {
      finalizarOperacion();
    }
  }

  async function recibir(compra: Compra): Promise<void> {
    if (operacionEnCurso.current || compra.estado !== "Borrador") return;

    const confirmado = window.confirm(
      `¿Confirmar la recepción de la compra #${compra.id}?\n\n` +
        `Total de la compra: ${pen(compra.totalPen)}.\n\n` +
        "Se registrarán las entradas de inventario y se actualizará " +
        "el costo promedio de los productos.\n\n" +
        "Después de recibirla, la compra no se podrá editar ni eliminar.",
    );

    if (!confirmado || !iniciarOperacion()) return;

    try {
      const recibida = await api<Compra>(
        `/compras/${compra.id}/recibir`,
        { method: "POST" },
      );

      setDetalle(recibida);
      setFormulario(false);
      setEditando(undefined);
      setMensaje(
        `Compra #${recibida.id} recibida correctamente. ` +
          "Se actualizaron las existencias y los costos promedio.",
      );

      await cargar();
    } catch (e) {
      const requiereConsulta =
        !(e instanceof ApiError) ||
        e.status === 409 ||
        e.status >= 500;

      if (requiereConsulta) {
        setDetalle(null);
        let estadoComprobado = false;

        try {
          const actual = await api<Compra>(`/compras/${compra.id}`);
          setDetalle(actual);
          estadoComprobado = true;
        } catch {
          // Se mantiene el detalle cerrado si no se pudo verificar.
        }

        await cargar();

        setError(
          `${mensajeError(e)} ${
            estadoComprobado
              ? "Se consultó nuevamente el estado de la compra; revísalo antes de continuar."
              : "No se pudo comprobar su estado. Consulta la compra antes de reintentar."
          }`,
        );
      } else {
        setError(mensajeError(e));
      }
    } finally {
      finalizarOperacion();
    }
  }

  return (
    <section className="space-y-5" aria-busy={bloqueado}>
      <h1 className="text-2xl font-semibold">
        Compras de importación
      </h1>

      <p>
        Origen del costo en USD · Costo puesto en tienda en PEN
      </p>

      <p className="text-sm text-slate-600">
        Guardar un borrador no modifica el inventario. Confirmar su
        recepción registra las entradas y actualiza el costo promedio.
      </p>

      <div className="flex flex-wrap gap-3">
        <Button disabled={bloqueado} onClick={nuevaCompra}>
          Nueva compra
        </Button>

        <Button
          variant="outline"
          disabled={bloqueado}
          onClick={() => {
            setError("");
            void cargar();
          }}
        >
          Actualizar lista
        </Button>
      </div>

      {mensaje && (
        <p role="status" className="rounded bg-green-50 p-3">
          {mensaje}
        </p>
      )}

      {error && (
        <p role="alert" className="rounded bg-red-50 p-3 text-red-700">
          {error}
        </p>
      )}

      {busy && <p role="status">Procesando operación…</p>}

      {formulario && (
        <CompraForm
          key={editando?.id ?? "nueva"}
          inicial={editando}
          busy={busy}
          onGuardar={guardar}
          onCancelar={() => {
            if (operacionEnCurso.current) return;
            setFormulario(false);
            setEditando(undefined);
          }}
          resumen={(datos) => <ResumenCompra datos={datos} />}
        />
      )}

      {loading && <p role="status">Cargando compras…</p>}

      <div className="overflow-x-auto">
        <table className="w-full text-left">
          <thead>
            <tr>
              <th>Compra</th>
              <th>Fecha</th>
              <th>Proveedor</th>
              <th>Estado</th>
              <th>Total PEN</th>
              <th>Acciones</th>
            </tr>
          </thead>

          <tbody>
            {lista.items.map((compra) => (
              <tr key={compra.id} className="border-b">
                <td className="py-3">#{compra.id}</td>
                <td>{compra.fecha.slice(0, 10)}</td>
                <td>#{compra.proveedorId}</td>
                <td>{compra.estado}</td>
                <td>{pen(compra.totalPen)}</td>
                <td>
                  <div className="flex flex-wrap gap-2 py-2">
                    <Button
                      variant="outline"
                      disabled={bloqueado}
                      onClick={() => void consultar(compra.id)}
                    >
                      Consultar #{compra.id}
                    </Button>

                    {compra.estado === "Borrador" && !formulario && (
                      <>
                        <Button
                          variant="outline"
                          disabled={bloqueado}
                          onClick={() => void editar(compra.id)}
                        >
                          Editar #{compra.id}
                        </Button>

                        <Button
                          variant="outline"
                          disabled={bloqueado}
                          onClick={() => void eliminar(compra.id)}
                        >
                          Eliminar #{compra.id}
                        </Button>

                        <Button
                          disabled={bloqueado}
                          onClick={() => void recibir(compra)}
                        >
                          Recibir #{compra.id}
                        </Button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {!loading && lista.count === 0 && (
        <p>No hay compras registradas.</p>
      )}

      <div className="flex items-center gap-3">
        <Button
          disabled={pagina === 1 || bloqueado || formulario}
          onClick={() => {
            setDetalle(null);
            setPagina((actual) => actual - 1);
          }}
        >
          Anterior
        </Button>

        <span>
          Página {pagina} · {lista.count} compras
        </span>

        <Button
          disabled={
            pagina * lista.tamano >= lista.count ||
            bloqueado ||
            formulario
          }
          onClick={() => {
            setDetalle(null);
            setPagina((actual) => actual + 1);
          }}
        >
          Siguiente
        </Button>
      </div>

      {detalle && (
        <article className="space-y-3 rounded border p-4">
          <h2 className="text-xl">
            Compra #{detalle.id} · {detalle.estado}
          </h2>

          {detalle.estado === "Recibida" ? (
            <p className="rounded bg-green-50 p-3">
              Compra recibida: solo consulta. Sus entradas de
              inventario ya fueron registradas.
            </p>
          ) : (
            <p>
              Compra en borrador: todavía no registra entradas de
              inventario.
            </p>
          )}

          <p>
            Mercadería: {usd(detalle.subtotalUsd)} →{" "}
            {pen(detalle.mercaderiaPen)} · TC {detalle.tipoCambio} PEN/USD
          </p>

          <p>
            Flete: {pen(detalle.fletePen)} · Aduana/agente:{" "}
            {pen(detalle.aduanaPen)} · Total confirmado:{" "}
            {pen(detalle.totalPen)}
          </p>

          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>Cantidad</th>
                  <th>Costo USD</th>
                  <th>Mercadería PEN</th>
                  <th>Flete PEN</th>
                  <th>Aduana PEN</th>
                  <th>Total línea PEN</th>
                  <th>Unitario PEN</th>
                </tr>
              </thead>

              <tbody>
                {detalle.lineas.map((linea) => (
                  <tr key={linea.productoId}>
                    <td>#{linea.productoId}</td>
                    <td>{linea.cantidad}</td>
                    <td>{usd(linea.costoUnitarioUsd)}</td>
                    <td>{pen(linea.mercaderiaPen)}</td>
                    <td>{pen(linea.fletePen)}</td>
                    <td>{pen(linea.aduanaPen)}</td>
                    <td>{pen(linea.totalPen)}</td>
                    <td>{pen(linea.costoUnitarioPuestoPen)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <p className="text-sm text-slate-600">
            Los unitarios mostrados están redondeados; el total utiliza
            los importes de cada línea confirmados por la API.
          </p>

          <div className="flex flex-wrap gap-3">
            {detalle.estado === "Borrador" && (
              <Button
                disabled={bloqueado}
                onClick={() => void recibir(detalle)}
              >
                Recibir compra #{detalle.id}
              </Button>
            )}

            <Button
              variant="outline"
              disabled={bloqueado}
              onClick={() => setDetalle(null)}
            >
              Cerrar detalle
            </Button>
          </div>
        </article>
      )}
    </section>
  );
}