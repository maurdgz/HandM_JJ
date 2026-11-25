import { useEffect, useState } from "react";

import { api } from "@/lib/api";
import { getToken, logout, saveUser } from "@/lib/session";
import type { UsuarioDto } from "@/lib/types";

import { Button } from "@/components/ui/button";
import { Toaster } from "@/components/ui/sonner";

import ComprasPage from "@/components/ComprasPage";
import CatalogosPage from "@/components/CatalogosPage";
import EmpleadosPage from "@/components/EmpleadosPage";
import PuntoVentaPage from "@/components/PuntoVentaPage";
import KardexPage from "@/components/KardexPage";
import StockPage from "@/components/StockPage";
import VentasPage from "@/components/VentasPage";

export type AppView =
  | "historial"
  | "ventas"
  | "compras"
  | "inicio"
  | "empleados"
  | "productos"
  | "categorias"
  | "proveedores"
  | "clientes"
  | "stock"
  | "kardex";

interface Props {
  view: AppView;
}

export default function AppShell({ view }: Props) {
  const [user, setUser] = useState<UsuarioDto | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!getToken()) {
      logout();
      return;
    }

    let active = true;

    api<UsuarioDto>("/auth/me")
      .then((current) => {
        if (!active) return;

        saveUser(current);
        setUser(current);
      })
      .catch((error: unknown) => {
        if (!active) return;

        setError(
          error instanceof Error
            ? error.message
            : "No se pudo comprobar la sesión.",
        );
      });

    return () => {
      active = false;
    };
  }, []);

  if (error) {
    return (
      <main className="p-8">
        <p role="alert">{error}</p>

        <Button
          className="mt-4"
          onClick={() => window.location.reload()}
        >
          Reintentar
        </Button>
      </main>
    );
  }

  if (!user) {
    return (
      <p className="p-8" role="status">
        Comprobando sesión.
      </p>
    );
  }

  const isAdmin = user.roles.includes("Administrador");
  const puedeLeerStock =
    isAdmin || user.roles.includes("Vendedor");

  return (
    <div className="min-h-screen md:grid md:grid-cols-[240px_1fr]">
      <aside className="no-print border-b bg-slate-50 p-6 md:border-b-0 md:border-r">
        <a href="/" className="text-xl font-semibold">
          ImportNet
        </a>

        <nav
          aria-label="Principal"
          className="mt-8 flex flex-wrap gap-4 md:flex-col"
        >
          <a href="/" className="rounded px-2 py-2 hover:bg-slate-200">
            Inicio
          </a>

          <a href="/productos">Productos</a>
          <a href="/categorias">Categorías</a>
          <a href="/clientes">Clientes</a>
          <a href="/ventas">Punto de venta</a>
          <a href="/ventas/historial">Historial de ventas</a>

          {puedeLeerStock && (
            <a href="/stock">Stock y alertas</a>
          )}

          {isAdmin && (
            <>
              <a href="/proveedores">Proveedores</a>
              <a href="/compras">Compras de importación</a>
              <a href="/kardex">Kardex y ajustes</a>

              <a
                href="/empleados"
                className="rounded px-2 py-2 hover:bg-slate-200"
              >
                Empleados
              </a>
            </>
          )}
        </nav>

        <div className="mt-8 border-t pt-5">
          <p className="text-sm font-medium">
            {user.nombre} {user.apellidos}
          </p>

          <p className="mt-1 text-sm text-slate-600">
            {user.roles.join(", ")}
          </p>

          <Button
            variant="outline"
            className="mt-4"
            onClick={logout}
          >
            Cerrar sesión
          </Button>
        </div>
      </aside>

      <main className="min-w-0 p-4 md:p-8">
        {view === "inicio" && (
          <>
            <h1 className="text-2xl font-semibold">
              Bienvenido, {user.nombre}
            </h1>

            <p className="mt-3 text-slate-600">
              Selecciona una opción del menú.
            </p>
          </>
        )}

        {view === "compras" && (
          isAdmin
            ? <ComprasPage />
            : (
              <p role="alert">
                Acceso restringido a Administrador.
              </p>
            )
        )}

        {view === "ventas" && <PuntoVentaPage />}
        {view === "historial" && <VentasPage user={user} />}

        {view === "empleados" && (
          isAdmin
            ? <EmpleadosPage />
            : <p role="alert">Acceso restringido.</p>
        )}

        {view === "stock" && (
          puedeLeerStock
            ? <StockPage admin={isAdmin} />
            : <p role="alert">No tienes permiso para consultar stock.</p>
        )}

        {view === "kardex" && (
          isAdmin
            ? <KardexPage />
            : <p role="alert">Acceso restringido a Administrador.</p>
        )}

        {(
          view === "productos" ||
          view === "categorias" ||
          view === "proveedores" ||
          view === "clientes"
        ) && (
          <CatalogosPage modulo={view} admin={isAdmin} />
        )}
      </main>

      <Toaster />
    </div>
  );
}
