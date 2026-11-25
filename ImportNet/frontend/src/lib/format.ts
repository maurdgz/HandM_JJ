export function moneda(value: number): string {
  return new Intl.NumberFormat("es-PE", {
    style: "currency", currency: "PEN",
  }).format(value);
}

export function fechaHora(value: string): string {
  return new Intl.DateTimeFormat("es-PE", {
    dateStyle: "short", timeStyle: "short", timeZone: "America/Lima",
  }).format(new Date(value));
}

export function fechaPeru(daysAgo = 0): string {
  return new Date(Date.now() - (5 * 60 * 60 + daysAgo * 24 * 60 * 60) * 1000)
    .toISOString().slice(0, 10);
}
