export interface FieldChange {
  field: string;
  old: string;
  new: string;
}

function show(value: unknown): string {
  if (value === null || value === undefined || value === '') return '–';
  return typeof value === 'object' ? JSON.stringify(value) : String(value);
}

/** Parses the audit "changes" JSON ({ field: { old, new } }) into display rows. */
export function parseChanges(json: string | null): FieldChange[] {
  if (!json) return [];
  try {
    const data = JSON.parse(json) as Record<string, { old?: unknown; new?: unknown }>;
    return Object.entries(data).map(([field, change]) => ({
      field,
      old: show(change?.old),
      new: show(change?.new),
    }));
  } catch {
    return [];
  }
}

export const entityTypeLabels: Record<string, string> = {
  User: 'Benutzer',
  Member: 'Mitglied',
  Payment: 'Zahlung',
  Invoice: 'Rechnung',
  OrganizationSettings: 'Einstellungen',
};

export const actionLabels: Record<string, string> = {
  'Auth.Login': 'Anmeldung',
  'Auth.LoginFailed': 'Anmeldung fehlgeschlagen',
  'Auth.Logout': 'Abmeldung',
  'Auth.PasswordChanged': 'Passwort geändert',
  'User.Created': 'Benutzer angelegt',
  'User.Updated': 'Benutzer geändert',
  'User.Deactivated': 'Benutzer deaktiviert',
  'User.Reactivated': 'Benutzer reaktiviert',
  'User.PasswordReset': 'Passwort zurückgesetzt',
  'Member.Created': 'Mitglied angelegt',
  'Member.Updated': 'Mitglied geändert',
  'Member.Deregistered': 'Mitglied abgemeldet',
  'Member.Reactivated': 'Mitglied reaktiviert',
  'Settings.Updated': 'Einstellungen geändert',
  'Payment.Recorded': 'Zahlung erfasst',
  'Payment.Cancelled': 'Zahlung storniert',
  'Invoice.Issued': 'Rechnung ausgestellt',
  'Invoice.Cancelled': 'Rechnung storniert',
  'Invoice.Resent': 'Rechnung erneut gesendet',
};
