const baseUrl = import.meta.env.VITE_API_URL as string | undefined;

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/** An error returned by the API, or a problem reaching it. */
export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'An unexpected error occurred.';
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | undefined;
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // The response was not JSON. A generic message is used below.
  }

  const fieldErrors = problem?.errors ?? {};
  const firstFieldMessage = Object.values(fieldErrors).flat()[0];
  const message =
    firstFieldMessage ?? problem?.detail ?? problem?.title ?? `The request failed (${response.status}).`;

  return new ApiError(message, response.status, fieldErrors);
}

/** Calls the API and returns the parsed JSON. Throws ApiError when the call fails. */
export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  if (!baseUrl) {
    throw new ApiError('VITE_API_URL is not set. Check the .env file and restart "npm run dev".', 0);
  }

  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body) headers.set('Content-Type', 'application/json');

  let response: Response;
  try {
    response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  } catch (error) {
    if (isAbortError(error)) throw error;
    throw new ApiError(
      'Cannot reach the server. Check that the API is running and that its https://localhost certificate is trusted.',
      0,
    );
  }

  if (!response.ok) throw await toApiError(response);
  return (await response.json()) as T;
}
