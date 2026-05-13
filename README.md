# 🔐 DevSecOps Demo Project

A full-stack web application demonstrating **DevSecOps principles** with a secure
.NET 8 Web API backend, React + TypeScript frontend, and a GitHub Actions CI/CD
pipeline with automated security gates.

---

## Project Structure

```
devsecops-demo/
├── backend/                        # .NET 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       # Register / Login endpoints
│   │   └── PostsController.cs      # CRUD posts (protected)
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core + SQLite
│   ├── DTOs/
│   │   └── Dtos.cs                 # Validated request/response records
│   ├── Middleware/
│   │   └── SecurityHeadersMiddleware.cs   # HTTP security headers
│   ├── Models/
│   │   ├── User.cs
│   │   └── Post.cs
│   ├── Services/
│   │   ├── AuthService.cs          # JWT generation, BCrypt hashing
│   │   └── PostService.cs          # Business logic + authorisation
│   ├── Tests/
│   │   ├── DevSecOpsApi.Tests.csproj
│   │   └── ServiceTests.cs         # xUnit tests
│   ├── Program.cs                  # App startup + DI + middleware pipeline
│   ├── appsettings.json
│   └── DevSecOpsApi.csproj
│
├── frontend/                       # React + TypeScript (Vite)
│   ├── src/
│   │   ├── api/
│   │   │   └── client.ts           # Secure API client (in-memory JWT)
│   │   ├── components/
│   │   │   └── Navbar.tsx
│   │   ├── contexts/
│   │   │   └── AuthContext.tsx     # Global auth state
│   │   ├── pages/
│   │   │   ├── AuthPages.tsx       # Login + Register
│   │   │   └── PostsPage.tsx       # Post list + CRUD
│   │   ├── test/
│   │   │   ├── setup.ts
│   │   │   └── validation.test.ts  # Vitest unit tests
│   │   ├── types/index.ts
│   │   ├── App.tsx
│   │   ├── main.tsx
│   │   └── index.css
│   ├── index.html                  # CSP meta tag
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
│
└── .github/
    └── workflows/
        └── ci.yml                  # Full DevSecOps pipeline
```

---

## Quick Start

### Prerequisites

| Tool          | Version  |
|---------------|----------|
| .NET SDK      | 8.0+     |
| Node.js       | 20+      |
| npm           | 10+      |

---

### 1 – Clone the repository

```bash
git clone https://github.com/<your-username>/devsecops-demo.git
cd devsecops-demo
```

### 2 – Run the Backend

```bash
cd backend
dotnet restore
dotnet run
```

The API starts on **http://localhost:5000**.  
Swagger UI is available at **http://localhost:5000/swagger**.

> **JWT key** – change `Jwt:Key` in `appsettings.json` before deploying.  
> In production always use an environment variable or secret vault:
> ```bash
> export Jwt__Key="your-super-secret-key-at-least-32-chars"
> ```

### 3 – Run the Frontend

```bash
cd frontend
npm install
npm run dev
```

The app starts on **http://localhost:5173** and proxies `/api` → `localhost:5000`.

### 4 – Run the Tests

**Backend:**
```bash
cd backend
dotnet test Tests/DevSecOpsApi.Tests.csproj --verbosity normal
```

**Frontend:**
```bash
cd frontend
npm test
```

---

## DevSecOps Principles Demonstrated

### 1. Shift-Left Security

Security checks happen **as early as possible** in the pipeline — before any
build artifact is produced.

```
Push → Dependency Scan → SAST (CodeQL) → Build → Tests → Deploy
         ↑ security gate    ↑ security gate
```

In `ci.yml`: the `backend` and `frontend` jobs run dependency scans **before**
compiling or bundling code.

---

### 2. Dependency Vulnerability Scanning

| Layer    | Tool                                    | Gate level |
|----------|-----------------------------------------|------------|
| .NET     | `dotnet list package --vulnerable`      | High/Critical → fail |
| Node.js  | `npm audit --audit-level=high`          | High/Critical → fail |

If a HIGH or CRITICAL CVE is found in any dependency, **the pipeline fails
immediately** and the PR/push cannot proceed.

---

### 3. Static Application Security Testing (SAST)

GitHub's **CodeQL** analyses both the C# backend and the TypeScript frontend for
common vulnerability patterns:

- SQL injection / injection attacks
- Insecure deserialization
- Path traversal
- XSS sinks
- Hardcoded credentials

Results appear in the **Security → Code scanning alerts** tab.

---

### 4. Secure API Design

| Practice | Implementation |
|---|---|
| Password hashing | BCrypt (adaptive, timing-safe) |
| Token-based auth | JWT with short expiry (60 min) |
| Input validation | Data Annotations on all DTOs |
| Generic error messages | Login returns "Invalid credentials" — not "user not found" |
| Authorisation checks | Service layer checks ownership before mutating data |
| HTTP security headers | `SecurityHeadersMiddleware` on every response |
| CORS allowlist | Only the known frontend origin is allowed |

---

### 5. Separation of Frontend and Backend Security

The frontend performs **client-side validation** for UX only.  
The backend **always re-validates** every field — never trusting the client.

```
Frontend validation → Better UX (fast feedback)
Backend validation  → Real security (cannot be bypassed)
```

The API client (`client.ts`) stores JWTs **in memory** (not `localStorage`)
to mitigate XSS-based token theft.

---

### 6. Security Gates in CI/CD

```yaml
# Pipeline blocks on:
# 1. Vulnerable .NET packages (High/Critical)
# 2. Vulnerable npm packages (High/Critical)
# 3. CodeQL findings
# 4. Failed unit tests
# 5. TypeScript type errors
# 6. ESLint violations

deploy:
  needs: [backend, frontend, codeql]   # ALL must pass
```

The deploy job only runs on `main` and only after every security and quality
gate has passed.

---

## API Reference

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | None | Register new user |
| POST | `/api/auth/login` | None | Login, receive JWT |
| GET | `/api/posts` | None | List all posts |
| GET | `/api/posts/{id}` | None | Get one post |
| POST | `/api/posts` | User | Create post |
| PUT | `/api/posts/{id}` | Owner/Admin | Update post |
| DELETE | `/api/posts/{id}` | Owner/Admin | Delete post |

---

## Security Checklist

- [x] Passwords hashed with BCrypt
- [x] JWT signed with HMAC-SHA256
- [x] JWT expiry enforced (60 minutes, no clock skew)
- [x] Generic auth error messages (no username enumeration)
- [x] Input validation on all endpoints
- [x] Ownership check before mutate/delete
- [x] HTTP security headers on every response
- [x] CORS restricted to known origins
- [x] Frontend JWT stored in memory (not localStorage)
- [x] Dependency scanning in CI (backend + frontend)
- [x] SAST with CodeQL in CI
- [x] Security gates block merge on failure

---

## Environment Variables (Production)

| Variable | Description |
|---|---|
| `Jwt__Key` | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | JWT issuer string |
| `Jwt__Audience` | JWT audience string |
| `ConnectionStrings__DefaultConnection` | SQLite or other DB connection string |

Never commit secrets. Use GitHub Actions Secrets or a vault in production.
