import { z } from 'zod';

// Client-side checks mirror the backend rules for fast feedback; the API remains authoritative.

export const requiredText = (max: number) =>
  z.string().trim().min(1, 'Dieses Feld ist erforderlich.').max(max, `Maximal ${max} Zeichen.`);

export const optionalText = (max: number) => z.string().trim().max(max, `Maximal ${max} Zeichen.`);

export const emailSchema = z
  .string()
  .trim()
  .min(1, 'Bitte die E-Mail-Adresse eingeben.')
  .max(254, 'Maximal 254 Zeichen.')
  .email('Bitte eine gültige E-Mail-Adresse eingeben.');

export const optionalEmailSchema = z.union([z.literal(''), emailSchema]);

/** NFR-SEC-03 */
export const passwordSchema = z
  .string()
  .min(10, 'Mindestens 10 Zeichen.')
  .regex(/\p{L}/u, 'Mindestens ein Buchstabe.')
  .regex(/\d/, 'Mindestens eine Ziffer.');

export const roleSchema = z.enum(['Admin', 'Cashier', 'Secretary'], { message: 'Bitte eine Rolle wählen.' });
