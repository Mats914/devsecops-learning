/**
 * Säker API-klient – DevSecOps-principer:
 * - Access token i minnet (inte localStorage → minskar risk vid XSS)
 * - Refresh token roteras automatiskt vid 401
 * - Centraliserad felhantering
 * - Alla anrop går via en funktion (inga utspridda fetch-anrop)
 */

import type {
  AuthResponse, LoginRequest, RegisterRequest,
  Post, PostsPagedResponse, CreatePostRequest, UpdatePostRequest,
  Comment, CreateCommentRequest, PageParams,
} from '../types';

const BASE = '/api';

// ── Token-lagring i minnet ──────────────────────────────────────────────────
let _accessToken:  string | null = null;
let _refreshToken: string | null = null;
let _refreshing:   Promise<boolean> | null = null; // förhindrar dubbla refresh-anrop

export const tokenStore = {
  setTokens: (access: string, refresh: string) => {
    _accessToken  = access;
    _refreshToken = refresh;
  },
  clearTokens: () => {
    _accessToken  = null;
    _refreshToken = null;
  },
  getAccess:  () => _accessToken,
  getRefresh: () => _refreshToken,
};

// ── Grundläggande fetch-wrapper ─────────────────────────────────────────────

async function request<T>(path: string, options: RequestInit = {}, retry = true): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string> ?? {}),
  };

  // Sätt inte Content-Type för FormData – webbläsaren sätter boundary själv
  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }

  // Skicka JWT om vi har en inloggad session
  if (_accessToken) headers['Authorization'] = `Bearer ${_accessToken}`;

  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers,
    credentials: 'same-origin',
  });

  // Token utgången? Försök refresha och kör om requesten en gång
  if (res.status === 401 && retry && _refreshToken) {
    const ok = await silentRefresh();
    if (ok) return request<T>(path, options, false);
  }

  // DELETE etc. kan returnera 204 utan body
  if (res.status === 204) return undefined as unknown as T;

  const body = await res.json().catch(() => ({}));
  if (!res.ok) throw { message: body?.message ?? 'Unexpected error.', status: res.status };
  return body as T;
}

// Tyst refresh i bakgrunden – användaren märker inget om det lyckas
async function silentRefresh(): Promise<boolean> {
  if (_refreshing) return _refreshing;
  _refreshing = (async () => {
    try {
      const res = await fetch(`${BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: _refreshToken }),
      });
      if (!res.ok) { tokenStore.clearTokens(); return false; }
      const data: AuthResponse = await res.json();
      tokenStore.setTokens(data.accessToken, data.refreshToken);
      return true;
    } catch {
      tokenStore.clearTokens();
      return false;
    } finally {
      _refreshing = null;
    }
  })();
  return _refreshing;
}

// ── Auth-endpoints ───────────────────────────────────────────────────────────

export const authApi = {
  register: (data: RegisterRequest) =>
    request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(data) }),

  login: (data: LoginRequest) =>
    request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(data) }),

  refresh: (refreshToken: string) =>
    request<AuthResponse>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) }),

  revoke: (refreshToken: string) =>
    request<void>('/auth/revoke', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
};

// ── Inlägg (posts) ───────────────────────────────────────────────────────────

export const postsApi = {
  getAll: ({ page = 1, pageSize = 10, search, author }: Partial<PageParams> = {}) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search)  params.set('search', search);
    if (author)  params.set('author', author);
    return request<PostsPagedResponse>(`/posts?${params}`);
  },

  getById: (id: number) => request<Post>(`/posts/${id}`),

  // Skapar inlägg med FormData så vi kan ladda upp bild samtidigt
  create: (data: CreatePostRequest, image?: File) => {
    const form = new FormData();
    form.append('title',   data.title);
    form.append('content', data.content);
    if (image) form.append('image', image);
    return request<Post>('/posts', { method: 'POST', body: form });
  },

  update: (id: number, data: UpdatePostRequest) =>
    request<Post>(`/posts/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  delete: (id: number) => request<void>(`/posts/${id}`, { method: 'DELETE' }),
};

// ── Kommentarer ──────────────────────────────────────────────────────────────

export const commentsApi = {
  getByPost: (postId: number) =>
    request<Comment[]>(`/posts/${postId}/comments`),

  create: (postId: number, data: CreateCommentRequest) =>
    request<Comment>(`/posts/${postId}/comments`, { method: 'POST', body: JSON.stringify(data) }),

  delete: (postId: number, commentId: number) =>
    request<void>(`/posts/${postId}/comments/${commentId}`, { method: 'DELETE' }),
};
