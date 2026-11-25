import { useEffect, useState, type SubmitEvent } from "react";
import { api } from "@/lib/api";
import type { Pagina } from "@/lib/compras";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface StockProducto {
  productoId: number;
  codigo: string;
  nombre: string;
  unidadMedida: string;
  almacenId: number;
  cantidad: number;
  stockMinimo: number;
  activo: boolean;
  stockBajo: boolean;
}

interface ConsultaStock {
  texto: string;
  stockBajo: boolean;
  pagina: number;
  tamano: number;
}

const formatoCantidad = new Intl.NumberFormat("es-PE", {
  maximumFractionDigits: 3,
});

interface Props {
  admin?: boolean;
}

export default function StockPage({ admin = false }: Props) {
  const [texto, setTexto] = useState("");
  const [stockBajo, setStockBajo] = useState(false);
  const [tamano, setTamano] = useState(20);
  const [consulta, setConsulta] = useState<ConsultaStock>({
    texto: "",
    stockBajo: false,
    pagina: 1,
    tamano: 20,
  });

  const [resultado, setResultado] =
    useState<Pagina<StockProducto> | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [recarga, setRecarga] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    async function cargar(): Promise<void> {
      setLoading(true);
      setError("");
      setResultado(null);

      const parametros = new URLSearchParams({
        texto: consulta.texto,
        stockBajo: String(consulta.stockBajo),
        pagina: String(consulta.pagina),
        tamano: String(consulta.tamano),
      });

      try {
        const datos = await api<Pagina<StockProducto>>(
          `/inventario/stock?${parametros.toString()}`,
          { signal: controller.signal },
        );

        if (controller.signal.aborted) return;

        const ultimaPagina = Math.max(
          1,
          Math.ceil(datos.count / consulta.tamano),
        );

        if (consulta.pagina > ultimaPagina) {
          setConsulta((actual) => ({
            ...actual,
            pagina: ultimaPagina,
          }));
          return;
        }

        setResultado(datos);
      } catch (e) {
        if (controller.signal.aborted) return;

        setError(
          e instanceof Error
            ? e.message
            : "No se pudieron consultar las existencias.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void cargar();

    return () => {
      controller.abort();
    };
  }, [consulta, recarga]);

  function buscar(event: SubmitEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (loading) return;

    setConsulta({
      texto: texto.trim(),
      stockBajo,
      pagina: 1,
      tamano,
    });
  }

  function limpiar(): void {
    if (loading) return;

    setTexto("");
    setStockBajo(false);
    setTamano(20);
    setConsulta({
      texto: "",
      stockBajo: false,
      pagina: 1,
      tamano: 20,
    });
  }

  const paginas = resultado
    ? Math.max(1, Math.ceil(resultado.count / consulta.tamano))
    : 1;

  const desde = resultado && resultado.count > 0
    ? (consulta.pagina - 1) * consulta.tamano + 1
    : 0;

  const hasta = resultado
    ? Math.min(consulta.pagina * consulta.tamano, resultado.count)
    : 0;

  return (
    <section className="space-y-5" aria-busy={loading}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">
          Existencias y stock bajo
        </h1>

        <Button
          variant="outline"
          disabled={loading}
          onClick={() => setRecarga((actual) => actual + 1)}
        >
          Actualizar
        </Button>
      </div>

      <p className="text-slate-600">
        Consulta de existencias del almacén principal.
      </p>

      <form onSubmit={buscar} className="rounded border bg-white p-4">
        <fieldset disabled={loading} className="space-y-4">
          <div className="grid gap-4 md:grid-cols-[1fr_180px]">
            <div className="space-y-2">
              <Label htmlFor="stock-texto">
                Buscar por nombre o código
              </Label>

              <Input
                id="stock-texto"
                type="search"
                maxLength={100}
                placeholder="Ejemplo: olla o IMP-001"
                value={texto}
                onChange={(event) => setTexto(event.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="stock-tamano">
                Productos por página
              </Label>

              <select
                id="stock-tamano"
                className="h-9 w-full rounded border px-3"
                value={tamano}
                onChange={(event) => setTamano(Number(event.target.value))}
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>

          <label
            htmlFor="stock-bajo"
            className="flex items-center gap-2"
          >
            <input
              id="stock-bajo"
              type="checkbox"
              checked={stockBajo}
              onChange={(event) => setStockBajo(event.target.checked)}
            />
            Mostrar solo productos con stock bajo
          </label>

          <div className="flex flex-wrap gap-3">
            <Button type="submit">Buscar</Button>

            <Button
              type="button"
              variant="outline"
              onClick={limpiar}
            >
              Limpiar filtros
            </Button>
          </div>
        </fieldset>
      </form>

      <p className="text-sm text-slate-600">
        La alerta se activa cuando la cantidad es menor o igual al
        stock mínimo. Los productos sin movimientos aparecen con
        cantidad cero.
      </p>

      {loading && (
        <p role="status">Consultando existencias…</p>
      )}

      {error && (
        <div role="alert" className="space-y-3 rounded bg-red-50 p-3">
          <p className="text-red-700">{error}</p>

          <Button
            variant="outline"
            disabled={loading}
            onClick={() => setRecarga((actual) => actual + 1)}
          >
            Reintentar consulta
          </Button>
        </div>
      )}

      {!loading && !error && resultado && (
        <>
          <p role="status">
            {resultado.count} productos encontrados
            {consulta.stockBajo ? " con stock bajo" : ""}
            {consulta.texto ? ` para “${consulta.texto}”` : ""}.
          </p>

          <div className="overflow-x-auto rounded border">
            <table className="w-full text-left">
              <caption className="sr-only">
                Existencias del almacén principal
              </caption>

              <thead className="bg-slate-50">
                <tr>
                  <th scope="col" className="px-3 py-3">Código</th>
                  <th scope="col" className="px-3 py-3">Producto</th>
                  <th scope="col" className="px-3 py-3">Unidad</th>
                  <th scope="col" className="px-3 py-3 text-right">
                    Cantidad
                  </th>
                  <th scope="col" className="px-3 py-3 text-right">
                    Stock mínimo
                  </th>
                  <th scope="col" className="px-3 py-3">
                    Disponibilidad
                  </th>
                  <th scope="col" className="px-3 py-3">
                    Producto activo
                  </th>
                  {admin && (
                    <th scope="col" className="px-3 py-3">
                      <span className="sr-only">Acciones</span>
                    </th>
                  )}
                </tr>
              </thead>

              <tbody>
                {resultado.items.map((producto) => (
                  <tr
                    key={producto.productoId}
                    className={
                      producto.stockBajo
                        ? "border-t bg-amber-50"
                        : "border-t"
                    }
                  >
                    <td className="px-3 py-3">{producto.codigo}</td>
                    <td className="px-3 py-3">{producto.nombre}</td>
                    <td className="px-3 py-3">{producto.unidadMedida}</td>

                    <td className="px-3 py-3 text-right">
                      {formatoCantidad.format(producto.cantidad)}
                    </td>

                    <td className="px-3 py-3 text-right">
                      {formatoCantidad.format(producto.stockMinimo)}
                    </td>

                    <td className="px-3 py-3">
                      <span
                        className={
                          producto.stockBajo
                            ? "rounded bg-amber-100 px-2 py-1 text-amber-900"
                            : "rounded bg-green-100 px-2 py-1 text-green-900"
                        }
                      >
                        {producto.stockBajo
                          ? producto.cantidad === 0
                            ? "Sin existencias"
                            : "Stock bajo"
                          : "Sobre el mínimo"}
                      </span>
                    </td>

                    <td className="px-3 py-3">
                      {producto.activo ? "Sí" : "No"}
                    </td>

                    {admin && (
                      <td className="px-3 py-3">
                        <a
                          className="text-blue-700 underline"
                          href={`/kardex?producto=${producto.productoId}`}
                        >
                          Kardex
                        </a>
                      </td>
                    )}
                  </tr>
                ))}

                {resultado.items.length === 0 && (
                  <tr>
                    <td colSpan={admin ? 8 : 7} className="px-3 py-6 text-center">
                      No se encontraron productos con estos filtros.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-slate-600">
              Mostrando {desde}–{hasta} de {resultado.count} productos.
            </p>

            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                disabled={loading || consulta.pagina <= 1}
                onClick={() => setConsulta((actual) => ({
                  ...actual,
                  pagina: actual.pagina - 1,
                }))}
              >
                Anterior
              </Button>

              <span>
                Página {consulta.pagina} de {paginas}
              </span>

              <Button
                variant="outline"
                disabled={
                  loading ||
                  consulta.pagina >= paginas ||
                  consulta.pagina >= 1000000
                }
                onClick={() => setConsulta((actual) => ({
                  ...actual,
                  pagina: actual.pagina + 1,
                }))}
              >
                Siguiente
              </Button>
            </div>
          </div>
        </>
      )}
    </section>
  );
}