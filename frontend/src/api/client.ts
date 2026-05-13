/**
 * Secure API client
 *
 * DevSecOps principle: All backend communication is centralised here.
 * - JWTs are stored in memory (NOT localStorage) to mitigate XSS token theft.
 * - The token is injected on every authenticated request automatically.
 * - Error handling is consistent; raw error details are never leaked to the UI.
 */

import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  Post,
  CreatePostRequest,
  UpdatePostRequest,
} from '../types';

// ── In-memory token store (mitigates XSS vs localStorage) ─────────────────
let _token: string | null = null;

export const tokenStore = {
  set: (token: string) => { _token = token; },
  get: ()              => _token,
  clear: ()            => { _token = null; },
};

// ── Base fetch wrapper ─────────────────────────────────────────────────────

const BASE_URL = '/api';

async function request<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> ?? {}),
  };

  if (_token) {
    headers['Authorization'] = `Bearer ${_token}`;
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
    // Security: never send cookies cross-origin
    credentials: 'same-origin',
  });

  if (response.status === 204) return undefined as unknown as T;

  const body = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw {
      message: body?.message ?? 'An unexpected error occurred.',
      status: response.status,
    };
  }

  return body as T;
}

// ── Auth endpoints ─────────────────────────────────────────────────────────

export const authApi = {
  register: (data: RegisterRequest) =>
    request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  login: (data: LoginRequest) =>
    request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
};

// ── Post endpoints ─────────────────────────────────────────────────────────

export const postsApi = {
  getAll: () =>
    request<Post[]>('/posts'),

  getById: (id: number) =>
    request<Post>(`/posts/${id}`),

  create: (data: CreatePostRequest) =>
    request<Post>('/posts', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  update: (id: number, data: UpdatePostRequest) =>
    request<Post>(`/posts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  delete: (id: number) =>
    request<void>(`/posts/${id}`, { method: 'DELETE' }),
};
