import { z } from 'zod';
import type { Member, MemberInput } from '../../api/types';
import { parseIsoDate, todayIso, toIsoDate } from '../../lib/format';
import { optionalEmailSchema, optionalText, requiredText } from '../../lib/schemas';

const maxAgeYears = 120;

function yearsAgo(years: number): string {
  const date = new Date();
  date.setFullYear(date.getFullYear() - years);
  return toIsoDate(date);
}

/** FR-MEM-01 field rules (mirrors the backend validator). */
export const memberSchema = z
  .object({
    firstName: requiredText(100),
    lastName: requiredText(100),
    dateOfBirth: z
      .string()
      .min(1, 'Bitte das Geburtsdatum eingeben.')
      .refine((v) => v < todayIso(), 'Das Geburtsdatum muss in der Vergangenheit liegen.')
      .refine((v) => v >= yearsAgo(maxAgeYears), `Das Alter darf höchstens ${maxAgeYears} Jahre betragen.`),
    street: requiredText(200),
    postalCode: requiredText(10),
    city: requiredText(100),
    country: optionalText(100),
    email: optionalEmailSchema,
    phone: optionalText(30),
    billingInterval: z.enum(['Monthly', 'Yearly'], { message: 'Bitte ein Zahlungsintervall wählen.' }),
    joinDate: z.string().min(1, 'Bitte das Eintrittsdatum eingeben.'),
    notes: optionalText(2000),
  })
  .refine((v) => !v.dateOfBirth || !v.joinDate || parseIsoDate(v.joinDate) >= parseIsoDate(v.dateOfBirth), {
    path: ['joinDate'],
    message: 'Das Eintrittsdatum darf nicht vor dem Geburtsdatum liegen.',
  });

export const memberFields = Object.keys(memberSchema.shape) as (keyof MemberInput)[];

export function emptyMember(): MemberInput {
  return {
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    street: '',
    postalCode: '',
    city: '',
    country: 'Österreich',
    email: '',
    phone: '',
    billingInterval: 'Monthly',
    joinDate: todayIso(),
    notes: '',
  };
}

export function toMemberInput(m: Member): MemberInput {
  return {
    firstName: m.firstName,
    lastName: m.lastName,
    dateOfBirth: m.dateOfBirth,
    street: m.street,
    postalCode: m.postalCode,
    city: m.city,
    country: m.country,
    email: m.email ?? '',
    phone: m.phone ?? '',
    billingInterval: m.billingInterval,
    joinDate: m.joinDate,
    notes: m.notes ?? '',
  };
}
