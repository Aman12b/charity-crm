import type { BillingInterval, Settings } from '../../api/types';
import { monthName, toIsoDate } from '../../lib/format';

export const monthOptions = Array.from({ length: 12 }, (_, i) => ({
  value: String(i + 1),
  label: monthName(i + 1),
}));

/** Contribution period for the member's billing interval (mirrors the backend's ContributionPeriod). */
export function periodFor(
  interval: BillingInterval,
  year: number,
  month: number | null,
): { start: string; end: string } {
  if (interval === 'Yearly' || month === null) {
    return { start: `${year}-01-01`, end: `${year}-12-31` };
  }
  return { start: toIsoDate(new Date(year, month - 1, 1)), end: toIsoDate(new Date(year, month, 0)) };
}

/** FR-PAY-01: the amount is prefilled from the fee settings. */
export function defaultAmount(
  interval: BillingInterval,
  settings: Pick<Settings, 'monthlyFee' | 'yearlyFee'>,
): number {
  return interval === 'Monthly' ? settings.monthlyFee : settings.yearlyFee;
}
