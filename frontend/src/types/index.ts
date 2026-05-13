// ── Auth ───────────────────────────────────────────────────────────────────

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
}

// ── Posts ──────────────────────────────────────────────────────────────────

export interface Post {
  id: number;
  title: string;
  content: string;
  authorUsername: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePostRequest {
  title: string;
  content: string;
}

export interface UpdatePostRequest {
  title: string;
  content: string;
}

// ── API error ──────────────────────────────────────────────────────────────

export interface ApiError {
  message: string;
  status?: number;
}
