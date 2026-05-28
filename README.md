# 🔐 DevSecOps Demo Project v2

Full-stack web application demonstrating **DevSecOps best practices** with:
- .NET 8 Web API backend
- React + TypeScript frontend
- GitHub Actions CI/CD pipeline with 4 security gates
- Docker + Docker Compose for containerized deployment

---

## What's New in v2

| Feature | Details |
|---|---|
| **Refresh Tokens** | Rotating JWT refresh tokens (7-day expiry) stored in memory |
| **Rate Limiting** | IP-based rate limiting on auth endpoints (10 attempts / 5 min) |
| **Password Strength** | Uppercase + lowercase + digit + special char required |
| **Comments** | Full CRUD on post comments with ownership checks |
| **Pagination** | `/api/posts?page=1&pageSize=10` |
| **Search & Filter** | `?search=keyword&author=username` |
| **Image Upload** | JPG/PNG upload with ImageSharp resize + MIME validation |
| **Email Verification** | MailKit integration (disabled by default) |
| **Audit Logging** | Every login/register/action logged with IP + timestamp |
| **Structured Logging** | Serilog JSON logs to console + rolling file |
| **Health Check** | `/health` endpoint for CI/CD smoke tests |
| **Docker** | Multi-stage Dockerfiles + docker-compose.yml |
| **Trivy Scan** | Container image vulnerability scanning in CI |
| **Smoke Test** | CI starts the real API and hits `/health` before deploy |

---

## Quick Start (Development)

### Prerequisites
| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Node.js | 20+ |

### Terminal 1 — Backend
```bash
cd backend
dotnet restore DevSecOpsApi.csproj
dotnet run --project DevSecOpsApi.csproj
# → http://localhost:5000
# → http://localhost:5000/swagger
```

### Terminal 2 — Frontend
```bash
cd frontend
npm install
npm run dev
# → http://localhost:5173
```

---

## Quick Start (Docker)

```bash
# Copy and edit env file
cp .env.example .env
# Edit JWT_KEY in .env

# Start everything
docker compose up --build

# App is at http://localhost:80
```

---

## Project Structure

```
devsecops-demo/
├── backend/
│   ├── Controllers/
│   │   ├── AuthController.cs       # register / login / refresh / revoke / verify-email
│   │   ├── PostsController.cs      # CRUD posts + image upload
│   │   ├── CommentsController.cs   # CRUD comments (nested under posts)
│   │   └── HealthController.cs     # /health endpoint
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── DTOs/
│   │   └── Dtos.cs                 # All request/response records with validation
│   ├── Middleware/
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Post.cs
│   │   ├── Comment.cs
│   │   ├── RefreshToken.cs
│   │   └── AuditLog.cs
│   ├── Services/
│   │   ├── AuthService.cs          # JWT + Refresh tokens + BCrypt
│   │   ├── PostService.cs          # Pagination + Search + Image
│   │   ├── CommentService.cs
│   │   ├── AuditService.cs         # Audit logging
│   │   ├── EmailService.cs         # MailKit (toggle via config)
│   │   └── ImageService.cs         # ImageSharp resize + validation
│   └── Tests/
│       ├── DevSecOpsApi.Tests.csproj
│       └── ServiceTests.cs         # 14 unit tests
│
├── frontend/
│   └── src/
│       ├── api/client.ts           # Secure API client + silent refresh
│       ├── contexts/AuthContext.tsx # Auth state + auto refresh timer
│       ├── hooks/usePosts.ts
│       ├── pages/
│       │   ├── AuthPages.tsx       # Login + Register + password strength meter
│       │   └── PostsPage.tsx       # Posts + Comments + Pagination + Search
│       └── test/
│           └── validation.test.ts  # 14 frontend unit tests
│
├── docker/
│   ├── Dockerfile.backend          # Multi-stage, non-root user
│   ├── Dockerfile.frontend         # Multi-stage, nginx
│   └── nginx.conf
├── docker-compose.yml
└── .github/workflows/ci.yml        # 6-job DevSecOps pipeline
```

---

## CI/CD Pipeline — 6 Security Gates

```
Push / PR
    │
    ├── Job 1: Backend
    │     ├── 🔒 Gate 1: dotnet vulnerability scan (High/Critical → FAIL)
    │     ├── Build
    │     └── 14 unit tests
    │
    ├── Job 2: Frontend
    │     ├── 🔒 Gate 2: npm audit (High/Critical → FAIL)
    │     ├── TypeScript type check
    │     ├── ESLint
    │     ├── 14 unit tests
    │     └── Production build
    │
    ├── Job 3: CodeQL SAST
    │     └── 🔒 Gate 3: Static analysis (C# + TypeScript)
    │
    ├── Job 4: Docker
    │     └── 🔒 Gate 4: Trivy container scan (High/Critical → FAIL)
    │
    ├── Job 5: Smoke Test
    │     └── 🔒 Gate 5: Start real API → /health must return 200
    │
    └── Job 6: Deploy (main branch only, ALL gates must pass)
          └── GitHub Pages
```

---

## API Reference

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register (username + password + optional email) |
| POST | `/api/auth/login` | — | Login → access + refresh token |
| POST | `/api/auth/refresh` | — | Rotate refresh token |
| POST | `/api/auth/revoke` | User | Revoke refresh token (logout) |
| GET  | `/api/auth/verify-email?token=` | — | Verify email address |
| GET  | `/api/posts?page&pageSize&search&author` | — | Paginated post list |
| GET  | `/api/posts/{id}` | — | Single post (increments view count) |
| POST | `/api/posts` (multipart) | User | Create post with optional image |
| PUT  | `/api/posts/{id}` | Owner/Admin | Update post |
| DELETE | `/api/posts/{id}` | Owner/Admin | Delete post |
| GET  | `/api/posts/{id}/comments` | — | List comments |
| POST | `/api/posts/{id}/comments` | User | Add comment |
| PUT  | `/api/posts/{id}/comments/{cid}` | Owner/Admin | Edit comment |
| DELETE | `/api/posts/{id}/comments/{cid}` | Owner/Admin | Delete comment |
| GET  | `/health` | — | Health check |

---

## Environment Variables (Production)

```bash
# Required
JWT_KEY=your-super-secret-key-min-32-chars

# Optional
Email__Enabled=true
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__Username=you@gmail.com
Email__Password=app-password
```

Never commit secrets. Use GitHub Secrets or a vault.

---

## DevSecOps Principles Applied

| Principle | Implementation |
|---|---|
| Shift-Left Security | Vulnerability scans run BEFORE build |
| Defence in Depth | Client + server validation; ownership checks at service layer |
| Least Privilege | Docker containers run as non-root user |
| Security Gates | 5 gates block unsafe builds from reaching deploy |
| Secure Defaults | HTTPOnly-equivalent in-memory token storage; short-lived JWTs |
| Audit Trail | Every sensitive action logged with IP and outcome |
| Zero Trust | Every API call re-validates JWT; frontend cannot be trusted |
| Container Security | Multi-stage builds; Trivy scan; minimal runtime image |
