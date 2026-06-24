// ── Auth – typer för inloggning och tokens ───────────────────────────────────

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  username: string;
  role: string;
  emailVerified: boolean;
  accessTokenExpiresAt: string;
}

export interface LoginRequest    { username: string; password: string; }
export interface RegisterRequest { username: string; password: string; email?: string; }

// ── Inlägg (posts) ───────────────────────────────────────────────────────────

export interface Post {
  id: number;
  title: string;
  content: string;
  imageUrl?: string;
  authorUsername: string;
  viewCount: number;
  commentCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface PostsPagedResponse {
  items: Post[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreatePostRequest { title: string; content: string; }
export interface UpdatePostRequest { title: string; content: string; }

// ── Kommentarer ──────────────────────────────────────────────────────────────

export interface Comment {
  id: number;
  content: string;
  authorUsername: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommentRequest { content: string; }

// ── Övrigt ───────────────────────────────────────────────────────────────────

export interface ApiError { message: string; status?: number; }

export interface PageParams {
  page: number;
  pageSize: number;
  search?: string;
  author?: string;
}
