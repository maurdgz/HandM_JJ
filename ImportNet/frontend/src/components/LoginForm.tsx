import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { api, ApiError } from "@/lib/api";
import { clearSession, saveToken, saveUser } from "@/lib/session";
import type { TokenDto, UsuarioDto } from "@/lib/types";

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

const schema = z.object({
  email: z.string().trim().email("Introduce un correo válido.").max(254),
  password: z.string().min(1, "Introduce tu contraseña.").max(1024),
});

type Values = z.infer<typeof schema>;

export default function LoginForm() {
  const [error, setError] = useState("");

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: "",
      password: "",
    },
  });

  async function submit(values: Values) {
    setError("");
    clearSession();

    try {
      const token = await api<TokenDto>("/auth/login", {
        method: "POST",
        auth: false,
        body: JSON.stringify(values),
      });

      saveToken(token);

      const user = await api<UsuarioDto>("/auth/me");
      saveUser(user);

      window.location.replace("/");
    } catch (error) {
      clearSession();

      setError(
        error instanceof ApiError && error.status === 401
          ? "Correo o contraseña incorrectos, o cuenta sin acceso."
          : error instanceof Error
            ? error.message
            : "No se pudo iniciar sesión.",
      );
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-slate-50 px-4">
      <section className="w-full max-w-md rounded-xl border bg-white p-8 shadow-sm">
        <p className="mb-2 text-sm text-slate-500">ImportNet</p>
        <h1 className="text-2xl font-semibold">Iniciar sesión</h1>
        <p className="mb-6 mt-2 text-sm text-slate-600">
          Ingresa con tu cuenta de empleado.
        </p>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(submit)} className="space-y-5">
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Correo</FormLabel>
                  <FormControl>
                    <Input
                      type="email"
                      autoComplete="username"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Contraseña</FormLabel>
                  <FormControl>
                    <Input
                      type="password"
                      autoComplete="current-password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {error && (
              <p role="alert" className="text-sm text-red-700">
                {error}
              </p>
            )}

            <Button
              type="submit"
              className="w-full"
              disabled={form.formState.isSubmitting}
            >
              {form.formState.isSubmitting ? "Ingresando…" : "Ingresar"}
            </Button>
          </form>
        </Form>
      </section>
    </main>
  );
}