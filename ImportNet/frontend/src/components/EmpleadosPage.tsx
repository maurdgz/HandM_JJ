import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { api, ApiError } from "@/lib/api";
import type { EmpleadoDto } from "@/lib/types";

import EmpleadoForm from "@/components/EmpleadoForm";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export default function EmpleadosPage() {
  const [rows, setRows] = useState<EmpleadoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<EmpleadoDto | null>(null);

  const [deleting, setDeleting] = useState<EmpleadoDto | null>(null);
  const [deletePending, setDeletePending] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setRows(await api<EmpleadoDto[]>("/empleados"));
    } catch (error) {
      setError(error instanceof Error ? error : new Error("No se pudo cargar."));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function removeEmployee() {
    if (!deleting) return;

    setDeletePending(true);

    try {
      await api<void>(`/empleados/${deleting.id}`, {
        method: "DELETE",
      });

      setDeleting(null);
      toast.success("Empleado desactivado.");
      await load();
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "No se pudo desactivar.",
      );
    } finally {
      setDeletePending(false);
    }
  }

  if (loading) {
    return <p role="status">Cargando empleados…</p>;
  }

  if (error instanceof ApiError && error.status === 403) {
    return (
      <section>
        <h1 className="text-2xl font-semibold">Acceso denegado</h1>
        <p role="alert" className="mt-3">
          La gestión de empleados requiere el rol Administrador.
        </p>
      </section>
    );
  }

  if (error) {
    return (
      <section>
        <p role="alert">{error.message}</p>
        <Button className="mt-4" onClick={() => void load()}>
          Reintentar
        </Button>
      </section>
    );
  }

  return (
    <section>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Empleados</h1>
          <p className="mt-1 text-sm text-slate-600">
            Administra cuentas, roles y acceso.
          </p>
        </div>

        <Button
          onClick={() => {
            setEditing(null);
            setFormOpen(true);
          }}
        >
          Nuevo empleado
        </Button>
      </div>

      {rows.length === 0 ? (
        <p>No hay empleados registrados.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Empleado</TableHead>
                <TableHead>Correo</TableHead>
                <TableHead>Roles</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Acciones</TableHead>
              </TableRow>
            </TableHeader>

            <TableBody>
              {rows.map((employee) => (
                <TableRow key={employee.id}>
                  <TableCell>
                    {employee.nombre} {employee.apellidos}
                  </TableCell>
                  <TableCell>{employee.email}</TableCell>
                  <TableCell>{employee.roles.join(", ")}</TableCell>
                  <TableCell>
                    {employee.activo ? "Activo" : "Inactivo"}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        onClick={() => {
                          setEditing(employee);
                          setFormOpen(true);
                        }}
                      >
                        Editar
                      </Button>

                      {employee.activo && (
                        <Button
                          variant="destructive"
                          onClick={() => setDeleting(employee)}
                        >
                          Eliminar
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {editing ? "Editar empleado" : "Nuevo empleado"}
            </DialogTitle>
            <DialogDescription>
              Completa los datos y selecciona los permisos.
            </DialogDescription>
          </DialogHeader>

          <EmpleadoForm
            key={editing?.id ?? "nuevo"}
            empleado={editing}
            onCancel={() => setFormOpen(false)}
            onSaved={async () => {
              setFormOpen(false);
              toast.success("Empleado guardado.");
              await load();
            }}
          />
        </DialogContent>
      </Dialog>

      <Dialog
        open={deleting !== null}
        onOpenChange={(open) => {
          if (!open && !deletePending) setDeleting(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Eliminar acceso del empleado</DialogTitle>
            <DialogDescription>
              Se desactivará a {deleting?.nombre} {deleting?.apellidos}.
              Se conservarán sus datos y su historial.
            </DialogDescription>
          </DialogHeader>

          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              disabled={deletePending}
              onClick={() => setDeleting(null)}
            >
              Cancelar
            </Button>

            <Button
              variant="destructive"
              disabled={deletePending}
              onClick={() => void removeEmployee()}
            >
              {deletePending ? "Desactivando…" : "Confirmar eliminación"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </section>
  );
}