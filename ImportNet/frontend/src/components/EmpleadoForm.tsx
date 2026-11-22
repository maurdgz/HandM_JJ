import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { api } from "@/lib/api";
import type { EmpleadoDto, Rol } from "@/lib/types";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";

interface Props {
  empleado: EmpleadoDto | null;
  onSaved: () => Promise<void>;
  onCancel: () => void;
  onPendingChange: (pending: boolean) => void;
}

const fields = [
  { name: "nombre", label: "Nombres", type: "text" },
  { name: "apellidos", label: "Apellidos", type: "text" },
  { name: "documentoIdentidad", label: "Documento", type: "text" },
  { name: "email", label: "Correo", type: "email" },
  { name: "telefono", label: "Teléfono", type: "tel" },
  { name: "cargo", label: "Cargo", type: "text" },
  { name: "area", label: "Área", type: "text" },
] as const;

const availableRoles: Rol[] = ["Administrador", "Vendedor"];

export default function EmpleadoForm({
  empleado,
  onSaved,
  onCancel,
  onPendingChange,
}: Props) {
  const [error, setError] = useState("");

  const schema = z.object({
    datos: z.object({
      nombre: z.string().trim().min(1, "Campo obligatorio.").max(200),
      apellidos: z.string().trim().min(1, "Campo obligatorio.").max(100),
      documentoIdentidad: z.string().trim()
        .regex(/^[a-z0-9-]{5,20}$/i, "Usa entre 5 y 20 letras, números o guiones."),
      email: z.string().trim().email("Correo inválido.").max(254),
      telefono: z.string().trim()
        .regex(/^\+?[0-9 ()-]{7,30}$/, "Teléfono inválido.")
        .refine(
          (value) => {
            const digits = value.replace(/\D/g, "").length;
            return digits >= 7 && digits <= 15;
          },
          "Indica entre 7 y 15 dígitos.",
        ),
      cargo: z.string().trim().min(1, "Campo obligatorio.").max(200),
      area: z.string().trim().min(1, "Campo obligatorio.").max(200),
    }),
    password: z.string(),
    activo: z.boolean(),
    roles: z.array(z.enum(["Administrador", "Vendedor"]))
      .min(1, "Selecciona al menos un rol."),
  }).superRefine((values, context) => {
    if (empleado) return;

    const password = values.password;

    if (
      password.length < 12 ||
      password.length > 128 ||
      !/[A-Z]/.test(password) ||
      !/[a-z]/.test(password) ||
      !/[0-9]/.test(password) ||
      !/[^a-zA-Z0-9]/.test(password)
    ) {
      context.addIssue({
        code: "custom",
        path: ["password"],
        message: "Usa entre 12 y 128 caracteres, mayúscula, minúscula, número y símbolo.",
      });
    }
  });

  type Values = z.infer<typeof schema>;

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      datos: {
        nombre: empleado?.nombre ?? "",
        apellidos: empleado?.apellidos ?? "",
        documentoIdentidad: empleado?.documentoIdentidad ?? "",
        email: empleado?.email ?? "",
        telefono: empleado?.telefono ?? "",
        cargo: empleado?.cargo ?? "",
        area: empleado?.area ?? "",
      },
      password: "",
      activo: empleado?.activo ?? true,
      roles: empleado?.roles ?? ["Vendedor"],
    },
  });

  async function submit(values: Values) {
    setError("");
    onPendingChange(true);

    try {
      if (empleado) {
        await api<void>(`/empleados/${empleado.id}`, {
          method: "PUT",
          body: JSON.stringify({
            datos: values.datos,
            activo: values.activo,
            roles: values.roles,
          }),
        });
      } else {
        await api<{ id: number }>("/empleados", {
          method: "POST",
          body: JSON.stringify(values),
        });
      }

      await onSaved();
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "No se pudo guardar.",
      );
    } finally {
      onPendingChange(false);
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(submit)} className="space-y-5">
        <fieldset
          disabled={form.formState.isSubmitting}
          className="space-y-5"
        >
          <div className="grid gap-4 sm:grid-cols-2">
            {fields.map(({ name, label, type }) => (
              <FormField
                key={name}
                control={form.control}
                name={`datos.${name}`}
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{label}</FormLabel>
                    <FormControl>
                      <Input type={type} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            ))}
          </div>

          {!empleado && (
            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Contraseña inicial</FormLabel>
                  <FormControl>
                    <Input
                      type="password"
                      autoComplete="new-password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          )}

          <FormField
            control={form.control}
            name="roles"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Roles</FormLabel>

                <div className="flex flex-wrap gap-5">
                  {availableRoles.map((role) => (
                    <label key={role} className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={field.value.includes(role)}
                        onChange={(event) => {
                          field.onChange(
                            event.target.checked
                              ? [...field.value, role]
                              : field.value.filter((value) => value !== role),
                          );
                        }}
                      />
                      {role}
                    </label>
                  ))}
                </div>

                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="activo"
            render={({ field }) => (
              <FormItem>
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={field.value}
                    onChange={(event) => field.onChange(event.target.checked)}
                  />
                  Empleado activo
                </label>
                <FormMessage />
              </FormItem>
            )}
          />

          {error && (
            <p role="alert" className="text-sm text-red-700">{error}</p>
          )}

          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancelar
            </Button>
            <Button type="submit">
              {form.formState.isSubmitting ? "Guardando…" : "Guardar"}
            </Button>
          </div>
        </fieldset>
      </form>
    </Form>
  );
}