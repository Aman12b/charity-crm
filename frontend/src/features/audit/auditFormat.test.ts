import { describe, expect, it } from 'vitest';
import { parseChanges } from './auditFormat';

describe('parseChanges (FR-AUD-02)', () => {
  it('turns the changes JSON into old → new rows', () => {
    expect(parseChanges('{"city":{"old":"Wien","new":"Graz"},"notes":{"old":null,"new":"x"}}')).toEqual([
      { field: 'city', old: 'Wien', new: 'Graz' },
      { field: 'notes', old: '–', new: 'x' },
    ]);
  });

  it('handles empty and invalid input', () => {
    expect(parseChanges(null)).toEqual([]);
    expect(parseChanges('kein json')).toEqual([]);
  });
});
