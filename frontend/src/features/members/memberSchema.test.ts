import { describe, expect, it } from 'vitest';
import { todayIso } from '../../lib/format';
import { emptyMember, memberSchema } from './memberSchema';

const valid = {
  ...emptyMember(),
  firstName: 'Anna',
  lastName: 'Muster',
  dateOfBirth: '1990-01-01',
  street: 'Gasse 1',
  postalCode: '1010',
  city: 'Wien',
};

describe('memberSchema (FR-MEM-01)', () => {
  it('accepts a valid member', () => {
    expect(memberSchema.safeParse(valid).success).toBe(true);
  });

  it('rejects a date of birth that is not in the past', () => {
    expect(memberSchema.safeParse({ ...valid, dateOfBirth: todayIso() }).success).toBe(false);
  });

  it('rejects an age above 120 years', () => {
    expect(memberSchema.safeParse({ ...valid, dateOfBirth: '1890-01-01' }).success).toBe(false);
  });

  it('requires name and address fields', () => {
    const result = memberSchema.safeParse({ ...valid, firstName: ' ', city: '' });
    expect(result.success).toBe(false);
    const paths = result.error!.issues.map((i) => i.path[0]);
    expect(paths).toEqual(expect.arrayContaining(['firstName', 'city']));
  });

  it('treats email as optional but validates it', () => {
    expect(memberSchema.safeParse({ ...valid, email: '' }).success).toBe(true);
    expect(memberSchema.safeParse({ ...valid, email: 'x' }).success).toBe(false);
  });
});
