import { notifications } from '@mantine/notifications';
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { ApiError } from '../api/client';

/**
 * Shows server-side validation errors on the matching form fields (backend validation is authoritative).
 * Returns true when at least one field error was applied.
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: string[],
) {
  if (!(error instanceof ApiError)) {
    return false;
  }
  let applied = false;
  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    if (fields.includes(key)) {
      setError(key as Path<T>, { type: 'server', message: messages.join(' ') });
      applied = true;
    }
  }
  return applied;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Ein unerwarteter Fehler ist aufgetreten.';
}

export function notifySuccess(message: string) {
  notifications.show({ color: 'teal', title: 'Gespeichert', message });
}

export function notifyError(error: unknown) {
  notifications.show({ color: 'red', title: 'Fehler', message: errorMessage(error) });
}

/** Default handler for form submits: field errors go to the fields, everything else becomes a notification. */
export function handleFormError<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: string[],
) {
  if (!applyServerErrors(error, setError, fields)) {
    notifyError(error);
  }
}
