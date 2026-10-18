import { describe, expect, it } from 'vitest';
import { optionalEmailSchema, passwordSchema } from './schemas';

describe('passwordSchema (NFR-SEC-03)', () => {
  it.each([
    ['Passwort12', true],
    ['Passwort1', false],
    ['nurbuchstaben', false],
    ['1234567890', false],
  ])('%s → %s', (password, valid) => {
    expect(passwordSchema.safeParse(password).success).toBe(valid);
  });
});

describe('optionalEmailSchema', () => {
  it('accepts an empty value and valid addresses only', () => {
    expect(optionalEmailSchema.safeParse('').success).toBe(true);
    expect(optionalEmailSchema.safeParse('a@b.at').success).toBe(true);
    expect(optionalEmailSchema.safeParse('keine-mail').success).toBe(false);
  });
});
