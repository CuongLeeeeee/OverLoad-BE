# OverLoad — E-Learning Platform API

A production-ready ASP.NET Core 8 Web API built with a clean **3-layer architecture**: API → Services → Repositories.

---

## 📁 Project Structure

```
OverLoad/
├── OverLoad.Domain/            # Pure C# entities & enums (no dependencies)
│   ├── Common/BaseEntity.cs
│   ├── Entities/               # User, Course, Lesson, Enrollment, UserLessonProgress
│   └── Enums/                  # UserRole, CourseLevel
│
├── OverLoad.Repositories/      # Data access layer (EF Core)
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── DesignTimeDbContextFactory.cs
│   ├── Configurations/         # IEntityTypeConfiguration per entity
│   ├── Interfaces/             # IBaseRepository<T> + domain-specific interfaces
│   └── Implementations/        # Concrete repository classes
│
├── OverLoad.Services/          # Business logic layer
│   ├── Common/ApiResponse.cs   # Unified response wrapper + pagination
│   ├── DTOs/
│   │   ├── Request/            # CreateXxx, UpdateXxx, XxxQueryParams
│   │   └── Response/           # XxxResponse, XxxDetailResponse
│   ├── Interfaces/             # IUserService, ICourseService, etc.
│   └── Implementations/        # Concrete service classes
│
└── OverLoad.API/               # ASP.NET Core Web API
    ├── Controllers/            # UsersController, CoursesController, etc.
    ├── Middleware/             # GlobalExceptionMiddleware
    ├── Program.cs              # DI wiring + Swagger + EF setup
    └── appsettings.json
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full — see connection string)

### 1. Clone & restore
```bash
git clone https://github.com/yourorg/overload.git
cd OverLoad
dotnet restore
```

### 2. Configure the database
Edit `OverLoad.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OverLoadDb_Dev;Trusted_Connection=True;"
  }
}
```

### 3. Apply EF Core migrations
```bash
# From the solution root:
dotnet ef migrations add InitialCreate \
  --project OverLoad.Repositories \
  --startup-project OverLoad.API

dotnet ef database update \
  --project OverLoad.Repositories \
  --startup-project OverLoad.API
```

> **Shortcut**: In Development mode the API auto-migrates on startup (`db.Database.MigrateAsync()`).

### 4. Run the API
```bash
cd OverLoad.API
dotnet run
```

Open **http://localhost:5000** — Swagger UI loads at the root.

---

## 📡 API Endpoints

### Users `/api/users`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users` | Paginated list (search, filter by role, sort) |
| GET | `/api/users/{id}` | User detail + enrollment history |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |

### Courses `/api/courses`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/courses` | Paginated list (search, category, level, published) |
| GET | `/api/courses/{id}` | Course detail + lessons |
| GET | `/api/courses/slug/{slug}` | Course by URL slug |
| GET | `/api/courses/{id}/lessons` | All ordered lessons for a course |
| POST | `/api/courses` | Create course (slug auto-generated) |
| PUT | `/api/courses/{id}` | Update course |
| DELETE | `/api/courses/{id}` | Delete course + cascade lessons |

### Lessons `/api/lessons`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/lessons` | Paginated list (filter by courseId, isFree, search) |
| GET | `/api/lessons/{id}` | Lesson detail with full content |
| POST | `/api/lessons` | Create lesson (auto-assigns OrderIndex) |
| PUT | `/api/lessons/{id}` | Update lesson / reorder |
| DELETE | `/api/lessons/{id}` | Delete (recalculates course totals) |

### Enrollments `/api/enrollments`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/enrollments` | Paginated list (filter by userId/courseId) |
| GET | `/api/enrollments/{id}` | Single enrollment |
| GET | `/api/enrollments/user/{userId}` | All enrollments for user |
| GET | `/api/enrollments/course/{courseId}` | All enrollments for course |
| POST | `/api/enrollments` | Enroll user in course |
| PATCH | `/api/enrollments/{id}/progress` | Update progress % |
| DELETE | `/api/enrollments/{id}` | Unenroll |

### Progress `/api/progress`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/progress/{id}` | Single progress record |
| GET | `/api/progress/user/{userId}` | All progress for user |
| GET | `/api/progress/user/{userId}/lesson/{lessonId}` | Progress for user+lesson |
| POST | `/api/progress` | Upsert progress (create or update) |
| DELETE | `/api/progress/{id}` | Delete progress record |

---

## 📦 Response Format

All endpoints return a consistent envelope:

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "errors": []
}
```

Paginated responses include:
```json
{
  "success": true,
  "message": "Data retrieved successfully",
  "data": [ ... ],
  "pagination": {
    "totalCount": 42,
    "page": 1,
    "pageSize": 10,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "errors": []
}
```

---

## 🔍 Query Parameters (example for `/api/courses`)

| Param | Type | Description |
|-------|------|-------------|
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 10, max: 100) |
| `search` | string | Search in title/description |
| `category` | string | Filter by category |
| `level` | string | `Beginner` / `Intermediate` / `Advanced` |
| `isPublished` | bool | Filter published/draft |
| `sortBy` | string | `title`, `category`, `totalLessons`, `createdAt` |
| `sortDesc` | bool | Sort descending (default: false) |

---

## 🏗️ Architecture Notes

- **Domain** has zero dependencies — pure POCO entities.
- **Repositories** depend only on Domain + EF Core.
- **Services** depend only on Domain + Repositories interfaces — fully testable with mocks.
- **API** wires everything via DI; controllers are thin delegates to services.
- Entity models are **never exposed directly** — all responses use DTOs.
- `GlobalExceptionMiddleware` catches unhandled exceptions and returns a structured `500` response.
- Course `TotalLessons` and `TotalDurationMinutes` are kept in sync automatically when lessons are created/updated/deleted.

---

## 🔧 Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 (Code First) |
| Database | SQL Server (LocalDB for dev) |
| API Docs | Swashbuckle / Swagger UI |
| Architecture | Repository + Service pattern |
