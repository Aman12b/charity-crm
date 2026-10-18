import { DateInput, type DateInputProps } from '@mantine/dates';
import { IconCalendar } from '@tabler/icons-react';

type DateFieldProps = Omit<DateInputProps, 'value' | 'onChange'> & {
  /** ISO date "yyyy-MM-dd" or "" */
  value: string;
  onChange: (value: string) => void;
};

/** Date input showing dd.MM.yyyy and exchanging ISO date strings with the form. */
export function DateField({ value, onChange, ...props }: DateFieldProps) {
  return (
    <DateInput
      valueFormat="DD.MM.YYYY"
      placeholder="TT.MM.JJJJ"
      leftSection={<IconCalendar size={16} />}
      dateParser={parseGermanDate}
      {...props}
      value={value || null}
      onChange={(v) => onChange(v ?? '')}
    />
  );
}

/** Accepts typed input like "5.1.2026" or "05.01.2026". */
function parseGermanDate(input: string): string | null {
  const match = /^\s*(\d{1,2})\.(\d{1,2})\.(\d{4})\s*$/.exec(input);
  if (!match) return null;
  const [, d, m, y] = match;
  const date = new Date(Number(y), Number(m) - 1, Number(d));
  if (date.getMonth() !== Number(m) - 1) return null;
  return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`;
}
