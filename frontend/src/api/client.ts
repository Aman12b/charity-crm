// Typed fetch wrapper: same-origin cookies, X-XSRF-TOKEN header (NFR-SEC-02) and ProblemDetails errors.

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? 'Ein unerwarteter Fehler ist aufgetreten.');
    this.status = status;
    this.code = problem.code;
    this.fieldErrors = problem.errors ?? {};
  }
}

let xsrfToken: string | null = null;
let unauthorizedHandler: (() => void) | null = null;

/** Called when any request (except login and the session probe) returns 401, e.g. after session expiry. */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

/** Antiforgery tokens are bound to the signed-in user; drop it whenever the identity changes. */
export function resetXsrfToken() {
  xsrfToken = null;
}

async function getXsrfToken(): Promise<string> {
  if (!xsrfToken) {
    const response = await fetch('/api/auth/antiforgery', { credentials: 'include' });
    xsrfToken = ((await response.json()) as { token: string }).token;
  }
  return xsrfToken;
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE';

async function send(path: string, method: Method, body: unknown): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (method !== 'GET') {
    headers['X-XSRF-TOKEN'] = await getXsrfToken();
    headers['Content-Type'] = 'application/json';
  }
  return fetch(`/api${path}`, {
    method,
    credentials: 'include',
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return {};
  }
}

async function request(path: string, method: Method, body?: unknown): Promise<Response> {
  let response = await send(path, method, body);

  if (response.status === 400 && method !== 'GET') {
    const problem = await readProblem(response.clone());
    if (problem.code === 'ANTIFORGERY_INVALID') {
      resetXsrfToken();
      response = await send(path, method, body);
    }
  }

  if (method !== 'GET' && path.startsWith('/auth/')) {
    resetXsrfToken();
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    if (response.status === 401 && path !== '/auth/login' && path !== '/auth/me') {
      unauthorizedHandler?.();
    }
    throw new ApiError(response.status, problem);
  }
  return response;
}

export async function apiGet<T>(path: string): Promise<T> {
  return (await (await request(path, 'GET')).json()) as T;
}

export async function apiPost<T = void>(path: string, body?: unknown): Promise<T> {
  const response = await request(path, 'POST', body ?? {});
  return (response.status === 204 ? undefined : await response.json()) as T;
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  return (await (await request(path, 'PUT', body)).json()) as T;
}

/** Downloads a file (PDF, CSV) through the authenticated session. */
export async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const response = await request(path, 'GET');
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const fileName = match ? decodeURIComponent(match[1]) : fallbackName;

  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/** Builds a query string, skipping empty values. */
export function toQuery(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  }
  const text = search.toString();
  return text ? `?${text}` : '';
}
