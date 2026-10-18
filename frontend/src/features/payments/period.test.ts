import { describe, expect, it } from 'vitest';
import { defaultAmount, monthOptions, periodFor } from './period';

describe('periodFor (FR-PAY-01)', () => {
  it('covers a whole calendar month for monthly payers', () => {
    expect(periodFor('Monthly', 2028, 2)).toEqual({ start: '2028-02-01', end: '2028-02-29' });
    expect(periodFor('Monthly', 2026, 12)).toEqual({ start: '2026-12-01', end: '2026-12-31' });
  });

  it('covers the calendar year for yearly payers', () => {
    expect(periodFor('Yearly', 2026, 5)).toEqual({ start: '2026-01-01', end: '2026-12-31' });
  });
});

describe('defaultAmount', () => {
  it('uses the fee matching the interval', () => {
    const settings = { monthlyFee: 10, yearlyFee: 110 };
    expect(defaultAmount('Monthly', settings)).toBe(10);
    expect(defaultAmount('Yearly', settings)).toBe(110);
  });
});

describe('monthOptions', () => {
  it('lists Austrian month names', () => {
    expect(monthOptions[0]).toEqual({ value: '1', label: 'Jänner' });
    expect(monthOptions).toHaveLength(12);
  });
});
