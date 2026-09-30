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

### Deploying the payment migration
`20260929082137_RefactorPaymentsAndWallet` converts existing payment data (see the summary at the top of the file):
types and methods are derived from the old system courses, wallet balances and PRO expiry are computed from past
transactions, and the `system-deposit-balance` / `pro-upgrade-*` courses and their enrollments are deleted.
**Back up the database first** — the migration cannot be rolled back (`Down()` throws). It was verified on
SQL Server LocalDB against a copy of the old schema with sample data.

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
| ORM / DB | Entity Framework Core 9 (Code First), SQL Server |
| Auth | JWT Bearer + rotating refresh tokens, BCrypt, role-based authorization |
| Validation | FluentValidation |
| API | Asp.Versioning (URL segment), Swagger / OpenAPI |
| Integrations | PayOS (payments), Google Gemini (AI chat) |
| Tests / CI | xUnit, EF Core InMemory, WebApplicationFactory, GitHub Actions |
