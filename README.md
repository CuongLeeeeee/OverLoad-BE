# OverLoad — E-Learning Platform API

ASP.NET Core 9 Web API with a layered architecture: API → Services → Repositories → Domain.

---

## 📁 Project Structure

```
OverLoad/
├── OverLoad.Domain/            # Pure C# entities & enums (no dependencies)
├── OverLoad.Repositories/      # EF Core: AppDbContext, configurations, repositories, migrations
├── OverLoad.Services/          # Business logic — knows nothing about HTTP
│   ├── Common/                 # PagedResult<T>, QueryParameters (paging + "-field" sorting)
│   ├── DTOs/                   # Service input (Request/*Request) and output (Response/*Dto)
│   ├── Exceptions/             # NotFound, Conflict, BusinessRule, Unauthorized, Forbidden, ExternalService
│   ├── Settings/               # JwtSettings, GeminiSettings, PaymentSettings (Options pattern)
│   └── Implementations/        # Services return DTOs and throw exceptions
├── OverLoad.API/
│   ├── Common/                 # ApiResponse<T> envelope, Roles, PayOSSettings
│   ├── Controllers/            # Thin controllers, versioned under /api/v1
│   ├── Models/                 # The public JSON contract, separate from service DTOs
│   │   ├── Requests/           #   *RequestModel (validated by FluentValidation)
│   │   ├── Responses/          #   *ResponseModel, PagedResponseModel<T>
│   │   └── Mappings/           #   ToServiceDto() / ToResponseModel() extension methods
│   ├── Infrastructure/         # PayOSGateway (PayOS SDK behind IPaymentGateway)
│   ├── Extensions/             # ClaimsPrincipal.GetUserId() / IsStaff()
│   ├── Middleware/             # GlobalExceptionMiddleware: exception → HTTP status + envelope
│   └── Validators/             # FluentValidation rules for the request models
└── tests/OverLoad.Tests/       # xUnit: service tests (EF InMemory) + HTTP integration tests
```

Request flow: `RequestModel` → validator → controller → `ToServiceDto()` → service → `*Dto` → `ToResponseModel()` → `ApiResponse<T>`.

---

## 🚀 Getting Started

### Option A — Docker (API + PostgreSQL, nothing else to install)
```bash
cp .env.example .env        # fill in JWT_SECRET_KEY (and PayOS / Gemini keys if needed)
docker compose up --build
```
API at http://localhost:8080 (Swagger at the root), PostgreSQL at `localhost:5432`. The database is
created and migrated on startup; data lives in the `overload-db-data` volume.

### Option B — .NET SDK

#### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 (local install, `docker run -e POSTGRES_PASSWORD=... -p 5432:5432 postgres:16-alpine`, or a free [Neon](https://neon.tech) database)

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

### Deployment settings

| Setting (env var form) | Default | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | — | PostgreSQL connection string, e.g. `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require` (Neon needs SSL) |
| `Jwt__SecretKey` | — (required) | Token signing key, 32+ characters |
| `Cors__AllowedOrigins__0`, `__1`, ... | none | Origins allowed to call the API from a browser. Development allows any origin when none are set; other environments allow **none** |
| `Payment__FrontendBaseUrl` | `http://localhost:3000` | Where PayOS sends buyers back to; return/cancel URLs must be on this origin |
| `Swagger__Enabled` | `false` | Serve Swagger outside Development |
| `Database__MigrateOnStartup` | `true` in Development, else `false` | Apply EF migrations when the app starts. Keep it off in production: back up, then run `dotnet ef database update` yourself |

The `Dockerfile` builds a slim ASP.NET runtime image listening on port **8080** as a non-root user.
Pass the settings above as environment variables; the image contains no `appsettings.json` or secrets.

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
| POST | `/checkout` | 🔑 | Start a PayOS payment: `{ "type": "Course", "courseId": 1 }` · `{ "type": "Pro", "package": "Month" \| "Year" }` · `{ "type": "Deposit", "amount": 100000 }`. Returns `orderCode` + `checkoutUrl` |
| GET | `/orders/{orderCode}` | 🔑 | Status of your order (refreshed from PayOS while pending) — call it from the return page |
| POST | `/webhook` | 🌐 | PayOS webhook (signature verified, idempotent). Also served at the legacy `/api/payment/webhook` |

### Wallet & history `/api/v1/me`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/wallet` | 🔑 | `{ balance, isPro, proExpiresAt }` |
| POST | `/wallet/purchase-pro` | 🔑 | `{ "package": "Month" \| "Year" }` — pay PRO from the wallet |
| GET | `/transactions` | 🔑 | Your payment history (`type`, `status` filters) |

### Admin `/api/v1/admin`
| Method | Route | Access | Description |
|--------|-------|--------|-------------|
| GET | `/transactions` | 👑 | All transactions (`userId`, `type`, `status` filters) |
| GET | `/revenue-stats` | 👑 | Revenue received through PayOS, courses / PRO sold, outstanding wallet balance |

**How payments work**
- A transaction has a `type` (`CoursePurchase`, `ProUpgrade`, `Deposit`), a `method` (`PayOS`, `Wallet`) and a `status` (`Pending`, `Success`, `Cancelled`). Amounts are always positive.
- An order is fulfilled exactly once — by the webhook, or by the status sync that runs on `/orders/{code}`, `/me/wallet` and `/me/transactions` when the webhook is late. The `status` column is a concurrency token, so a race between the two cannot enroll or credit twice.
- `Users.WalletBalance` is a concurrency token too: two simultaneous PRO purchases cannot overdraw the wallet.
- **PRO** (`Users.ProExpiresAt`) gives access to every paid course until it expires. Buying again while active extends from the current expiry. PRO users can also `POST /courses/{id}/enroll` on paid courses to track progress there.
- Prices, deposit limits and the frontend URL come from the `Payment` settings. Client-supplied `returnUrl` / `cancelUrl` must be on `FrontendBaseUrl`.

### Accounts and data mapped from the old production database
- `Users.IsLocked` is enforced: locked accounts get **403** on login and refresh. Admins use
  `POST /api/v1/users/{id}/lock` and `/unlock` (locking also revokes the refresh token).
- The student-verification and AI-limit columns and the `BugReports` table (created in the old SQL Server
  database by migrations whose source was lost) are part of the model; their **features** are not rebuilt yet.

### Database: PostgreSQL
The project runs on PostgreSQL (Npgsql). `20260930074654_InitialPostgres` creates the whole schema in one step;
the SQL Server migrations it replaces are in the history of the `refactor/payments` branch.
- All `DateTime` columns are `timestamp with time zone` and are always UTC (API responses end in `Z`).
- Text search (`search=` on courses, lessons, users) is case-insensitive.
- Moving the existing SQL Server data is a one-off job for `tools/OverLoad.DataTransfer` (below).

### Hosting: Render + Neon
`render.yaml` describes the API as a Render web service (Docker, free plan, Singapore, health check on
`/health`, auto-deploy from the configured branch). In Render: **New → Blueprint**, pick this repo, then fill in
the secrets it asks for (`DATABASE_URL` = Neon **pooled** connection string, CORS origin, PayOS and Gemini keys).
`Jwt__SecretKey` is generated by Render. Free instances sleep after ~15 minutes idle; the first request after
that takes 30–60 s. A late PayOS webhook is covered by the status sync on `/payment/orders/{code}`.

### One-off data transfer: SQL Server (Azure) → PostgreSQL (Neon)
`tools/OverLoad.DataTransfer` copies every table from the old SQL Server database into a PostgreSQL
database that already has the `InitialPostgres` schema, converting payment data on the way
(`LegacyTransform`: transaction type/method/PRO package from the old system courses, positive amounts,
wallet balances, PRO expiry, UTC dates; system courses and their enrollments are dropped).

1. Put both connections in `.env` (never commit it):
   `SOURCE_SQLSERVER_CONNECTION=Server=tcp:...;Initial Catalog=...;User ID=...;Password=...;Encrypt=True;`
   and `DATABASE_URL=postgresql://...` (Neon: use the direct host, not `-pooler`).
2. Create the schema on the target: `dotnet ef database update --project OverLoad.Repositories --startup-project OverLoad.API`
3. Dry run — writes everything in one transaction, checks row counts and constraints, then **rolls back**:
   `dotnet run --project tools/OverLoad.DataTransfer`
4. Commit: `dotnet run --project tools/OverLoad.DataTransfer -- --apply`

The source is only read (`SELECT`, in a snapshot transaction on Azure SQL). The tool refuses to run when a
target table already has rows, and stops instead of dropping a source column that has no place in the target.
Verified end to end: an old-schema SQL Server database with sample data → PostgreSQL 16 → the API.

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
| `POST /api/payment/create-link` `{ courseId }` | `POST /api/v1/payment/checkout` `{ type: "Course", courseId }` |
| `POST /api/payment/create-pro-link` `{ packageType: "month" }` | `POST /api/v1/payment/checkout` `{ type: "Pro", package: "Month" }` |
| `POST /api/payment/create-deposit-link` `{ amount }` | `POST /api/v1/payment/checkout` `{ type: "Deposit", amount }` |
| response `{ checkoutUrl }` | `data: { orderCode, amount, checkoutUrl }` |
| `POST /api/payment/buy-pro-balance` | `POST /api/v1/me/wallet/purchase-pro` `{ package }` |
| `GET /api/payment/balance` → `{ balance }` | `GET /api/v1/me/wallet` → `data: { balance, isPro, proExpiresAt }` |
| `GET /api/payment/transactions` (own; staff saw all) | `GET /api/v1/me/transactions` (own) · `GET /api/v1/admin/transactions` (Admin) |
| `GET /api/payment/stats` | `GET /api/v1/admin/revenue-stats` (Admin) |
| transaction `status: "SUCCESS"`, negative `amount` for wallet spending | `status: "Success"`, positive `amount` + `method: "Wallet"` |
| PRO = enrollment in a hidden `pro-upgrade-*` course | `isPro` / `proExpiresAt` on `/me` and `/me/wallet` |

---

## 🔧 Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 9 |
| ORM / DB | Entity Framework Core 9 (Code First), PostgreSQL (Npgsql) |
| Auth | JWT Bearer + rotating refresh tokens, BCrypt, role-based authorization |
| Validation | FluentValidation |
| API | Asp.Versioning (URL segment), Swagger / OpenAPI |
| Integrations | PayOS (payments), Google Gemini (AI chat) |
| Tests / CI | xUnit, EF Core InMemory, WebApplicationFactory, GitHub Actions |
