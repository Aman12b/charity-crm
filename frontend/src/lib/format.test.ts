import { describe, expect, it } from 'vitest';
import { formatCurrency, formatDate, formatDateTime, formatPeriod, parseIsoDate, toIsoDate } from './format';

describe('format (NFR-UX-01)', () => {
  it('formats dates as dd.MM.yyyy', () => {
    expect(formatDate('2026-01-05')).toBe('05.01.2026');
  });

  it('formats amounts as 1.234,56 €', () => {
    expect(formatCurrency(1234.56)).toBe('1.234,56\u00a0€');
  });

  it('shows a dash for missing values', () => {
    expect(formatDate(null)).toBe('–');
    expect(formatCurrency(undefined)).toBe('–');
  });

  it('converts UTC timestamps to Vienna time (NFR-DATA-03)', () => {
    expect(formatDateTime('2026-07-01T10:00:00Z')).toBe('01.07.2026, 12:00');
  });

  it('round-trips ISO dates without time-zone shift', () => {
    expect(toIsoDate(parseIsoDate('2026-03-31'))).toBe('2026-03-31');
  });

  it('formats yearly and monthly periods', () => {
    expect(formatPeriod('2026-01-01', '2026-12-31')).toBe('2026');
    expect(formatPeriod('2026-01-01', '2026-01-31')).toBe('Jänner 2026');
  });
});
