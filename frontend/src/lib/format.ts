// Formatting helpers for Austrian German (NFR-UX-01, NFR-DATA-03).

const dateFormat = new Intl.DateTimeFormat('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' });
const dateTimeFormat = new Intl.DateTimeFormat('de-AT', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'Europe/Vienna',
});
// de-AT groups thousands with a space; the requirement asks for "1.234,56 €", which is the de-DE pattern.
const currencyFormat = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' });
const monthFormat = new Intl.DateTimeFormat('de-AT', { month: 'long', year: 'numeric' });
const monthNameFormat = new Intl.DateTimeFormat('de-AT', { month: 'long' });

/** Parses an ISO date ("yyyy-MM-dd") as a local calendar date, avoiding time-zone shifts. */
export function parseIsoDate(value: string): Date {
  const [y, m, d] = value.slice(0, 10).split('-').map(Number);
  return new Date(y, m - 1, d);
}

/** Formats a Date as ISO date ("yyyy-MM-dd") using its local calendar date. */
export function toIsoDate(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function todayIso(): string {
  return toIsoDate(new Date());
}

/** "2026-01-05" → "05.01.2026" */
export function formatDate(value: string | null | undefined): string {
  return value ? dateFormat.format(parseIsoDate(value)) : '–';
}

/** UTC timestamp → "05.01.2026, 14:30" in Europe/Vienna */
export function formatDateTime(value: string | null | undefined): string {
  return value ? dateTimeFormat.format(new Date(value)) : '–';
}

/** 1234.56 → "1.234,56 €" */
export function formatCurrency(value: number | null | undefined): string {
  return value == null ? '–' : currencyFormat.format(value);
}

/** Contribution period: "Jänner 2026" for months, "2026" for years. */
export function formatPeriod(periodStart: string, periodEnd: string): string {
  const start = parseIsoDate(periodStart);
  const end = parseIsoDate(periodEnd);
  if (start.getMonth() === 0 && end.getMonth() === 11 && start.getFullYear() === end.getFullYear()) {
    return String(start.getFullYear());
  }
  return monthFormat.format(start);
}

export function monthName(month: number): string {
  return monthNameFormat.format(new Date(2000, month - 1, 1));
}
