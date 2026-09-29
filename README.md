# OverLoad — E-Learning Platform API

ASP.NET Core 9 Web API with a layered architecture: API → Services → Repositories → Domain.

---

## 📁 Project Structure

```
OverLoad/
├── OverLoad.Domain/            # Pure C# entities & enums (no dependencies)
├── OverLoad.Repositories/      # EF Core: AppDbContext, configurations, repositories, migrations
├── OverLoad.Services/          # Business logic
│   ├── Common/                 # PagedResult<T>, QueryParameters (paging + "-field" sorting)
│   ├── DTOs/                   # Request / Response DTOs
│   ├── Exceptions/             # NotFound, Conflict, BusinessRule, Unauthorized, Forbidden, ExternalService
│   ├── Settings/               # JwtSettings, GeminiSettings (Options pattern)
│   └── Implementations/        # Services return DTOs and throw exceptions — no HTTP concerns
├── OverLoad.API/
│   ├── Common/                 # ApiResponse<T> envelope, Roles, PayOSSettings
│   ├── Controllers/            # Thin controllers, versioned under /api/v1
│   ├── Extensions/             # ClaimsPrincipal.GetUserId() / IsStaff()
│   ├── Middleware/             # GlobalExceptionMiddleware: exception → HTTP status + envelope
│   └── Validators/             # FluentValidation rules
└── tests/OverLoad.Tests/       # xUnit: service tests (EF InMemory) + HTTP integration tests
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full)

### 1. Configure
Copy `appsettings.example.json` to `OverLoad.API/appsettings.json` and fill in the values.
Every setting can also come from environment variables, e.g. `Jwt__SecretKey`, `PayOS__ApiKey`
(the legacy `PAYOS_CLIENT_ID` / `PAYOS_API_KEY` / `PAYOS_CHECKSUM_KEY` variables still work).

### 2. Database
```bash
dotnet ef database update --project OverLoad.Repositories --startup-project OverLoad.API
```
In Development the API also migrates automatically on startup.

### 3. Run
```bash
dotnet run --project OverLoad.API
```
Swagger UI is served at the root in Development. `GET /health` returns `{ "status": "healthy" }`.

### 4. Test
```bash
dotnet test
```
No database is needed — tests use EF Core InMemory. CI runs the same command on every PR (`.github/workflows/ci.yml`).

---

## 📦 Response Format

Every endpoint — including validation errors, 401 and 403 — returns the same envelope:

```json
{ "success": true, "message": "Request processed successfully", "data": { }, "errors": null }
```

Paged endpoints put the page inside `data`:

```json
{
  "success": true,
  "data": {
    "items": [ ],
    "pageNumber": 1, "pageSize": 10, "totalCount": 42, "totalPages": 5,
    "hasPreviousPage": false, "hasNextPage": true
  }
}
```

| Status | When |
|--------|------|
| 400 | Validation failed (`message: "Validation failed."`, `errors: [...]`) or a business rule was broken |
| 401 | Missing / invalid token, wrong credentials |
| 403 | Signed in but not allowed (wrong role, not enrolled) |
| 404 | Resource not found (draft courses are reported as 404 to non-staff) |
| 409 | Duplicate (email already used, already enrolled) |
| 503 | AI service unavailable |

Enums are sent and received as names: `"level": "Beginner"`, `"role": "Instructor"`.

**Query parameters** for list endpoints: `pageNumber` (default 1), `pageSize` (default 10, max 100),
`sortBy` — a field name, prefixed with `-` for descending (e.g. `sortBy=-createdAt`).

---

## 📡 API Endpoints (v1)

🌐 public · 🔑 signed in · 🎓 Admin or Instructor · 👑 Admin

### Auth `/api/v1/auth`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| POST | `/register` | 🌐 | Create a Student account, returns tokens |
| POST | `/login` | 🌐 | Returns access + refresh token |
| POST | `/refresh` | 🌐 | Rotate refresh token, new access token |
| POST | `/logout` | 🔑 | Revoke refresh token |

### Me `/api/v1/me` — the user id always comes from the token
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/` | 🔑 | Profile + enrollment history |
| PUT | `/` | 🔑 | Update name / avatar / bio |
| GET | `/courses` | 🔑 | Enrolled courses with progress |
| GET | `/courses/{courseId}/progress` | 🔑 | Progress summary in one course |
| GET | `/lessons/{lessonId}/progress` | 🔑 | Progress on one lesson |
| PUT | `/lessons/{lessonId}/progress` | 🔑 | Save video position / scroll / checkpoints (upsert) |

### Courses `/api/v1/courses`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/` | 🌐 | Paged list (`search`, `category`, `level`, `isPublished`*) |
| GET | `/{id}` · `/slug/{slug}` | 🌐 | Course detail + lesson outline |
| GET | `/category/{category}` | 🌐 | Published courses of a category |
| GET | `/{id}/lessons` | 🌐 | Lessons with lock state (+ your progress when signed in) |
| POST | `/{id}/enroll` | 🔑 | Enroll yourself in a **free** course |
| POST · PUT · DELETE | `/` · `/{id}` | 🎓 | Manage courses |

\* `isPublished=false` only has effect for staff; everyone else always gets published courses.

### Lessons `/api/v1/lessons`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/{id}` | 🌐 / 🔑 | Full content. Free lessons are public; paid lessons need an enrollment |
| POST | `/{id}/complete` | 🔑 | Mark completed; course % is recalculated on the server |
| GET | `/` | 🎓 | Paged list with content |
| POST · PUT · DELETE | `/` · `/{id}` | 🎓 | Manage lessons (course totals kept in sync) |

### Enrollments `/api/v1/enrollments`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/` · `/{id}` | 🎓 | Paged list (`userId`, `courseId`) / detail |
| POST | `/` | 👑 | Grant access to any course without payment |
| DELETE | `/{id}` | 👑 | Remove an enrollment |

### Progress `/api/v1/progress`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/` | 🎓 | Paged progress records (`userId`, `lessonId`, `completed`) |

### Payment `/api/v1/payment`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| POST | `/create-link` · `/create-pro-link` · `/create-deposit-link` | 🔑 | PayOS checkout links |
| POST | `/buy-pro-balance` | 🔑 | Buy PRO with wallet balance |
| GET | `/balance` · `/transactions` | 🔑 | Wallet balance / own transactions (staff see all) |
| GET | `/stats` | 👑 | Revenue statistics |
| POST | `/webhook` | 🌐 | PayOS webhook (signature verified). Also served at the legacy `/api/payment/webhook` |

> Payment responses do not use the envelope yet — that is part of the payment refactor (phase 3).

### Chat `/api/v1/chat`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| POST | `/` | 🔑 | AI coding assistant (Gemini), with prompt-injection guard |

---

## 🔁 Migrating from the unversioned API

| Before | Now |
|--------|-----|
| `/api/...` | `/api/v1/...` |
| `data: [...]` + `pagination: {...}` | `data: { items: [...], pageNumber, ... }` |
| `?page=2&sortBy=title&sortDesc=true` | `?pageNumber=2&sortBy=-title` |
| `GET /api/auth/me` | `GET /api/v1/me` |
| `GET /api/users/me/courses` | `GET /api/v1/me/courses` |
| `GET /api/courses/{id}/progress` | `GET /api/v1/me/courses/{id}/progress` |
| `POST /api/enrollments` (anyone) | `POST /api/v1/courses/{id}/enroll` (self, free courses) · `POST /api/v1/enrollments` (Admin) |
| `GET /api/enrollments/user/{userId}` | `GET /api/v1/me/courses` · `GET /api/v1/enrollments?userId=` (staff) |
| `PATCH /api/enrollments/{id}/progress` | removed — progress is calculated on the server |
| `POST /api/progress/upsert`, `POST/PUT /api/progress` | `PUT /api/v1/me/lessons/{lessonId}/progress` |
| `GET /api/progress/user/{userId}/lesson/{lessonId}` | `GET /api/v1/me/lessons/{lessonId}/progress` |
| `POST /api/lessons/{id}/complete` with `{ courseProgress }` | same route under `/api/v1`, **no body** |
| `level` / `role` sent as strings | unchanged on the wire; now validated as enums |

---

## 🔧 Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 9 |
| ORM / DB | Entity Framework Core 9 (Code First), SQL Server |
| Auth | JWT Bearer + rotating refresh tokens, BCrypt, role-based authorization |
| Validation | FluentValidation |
| API | Asp.Versioning (URL segment), Swagger / OpenAPI |
| Integrations | PayOS (payments), Google Gemini (AI chat) |
| Tests / CI | xUnit, EF Core InMemory, WebApplicationFactory, GitHub Actions |
