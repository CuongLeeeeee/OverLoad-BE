using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;

namespace OverLoad.Repositories.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Run pending migrations first
        await context.Database.MigrateAsync();

        // Seed in order (respect FK dependencies)
        await SeedUsersAsync(context);
        await SeedCoursesAsync(context);
        await SeedLessonsAsync(context);
        await SeedEnrollmentsAsync(context);
        await SeedProgressAsync(context);
    }

    // ── Users ────────────────────────────────────────────────────────────────

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync()) return;

        var users = new List<User>
        {
            new()
            {
                Email = "admin@overload.io",
                PasswordHash = HashPassword("Admin@123"),
                FullName = "System Admin",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=SA",
                Bio = "Platform administrator.",
                Role = UserRole.Admin,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "john.instructor@overload.io",
                PasswordHash = HashPassword("Instructor@123"),
                FullName = "John Carter",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=JC",
                Bio = "Senior software engineer with 10+ years of experience in .NET and cloud architecture.",
                Role = UserRole.Instructor,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "sarah.instructor@overload.io",
                PasswordHash = HashPassword("Instructor@123"),
                FullName = "Sarah Mitchell",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=SM",
                Bio = "Full-stack developer and React enthusiast. Passionate about teaching modern web development.",
                Role = UserRole.Instructor,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "alice@student.com",
                PasswordHash = HashPassword("Student@123"),
                FullName = "Alice Johnson",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=AJ",
                Bio = "Aspiring backend developer.",
                Role = UserRole.Student,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "bob@student.com",
                PasswordHash = HashPassword("Student@123"),
                FullName = "Bob Williams",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=BW",
                Bio = "Computer science student interested in full-stack development.",
                Role = UserRole.Student,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "charlie@student.com",
                PasswordHash = HashPassword("Student@123"),
                FullName = "Charlie Brown",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=CB",
                Bio = "Self-taught developer transitioning from finance.",
                Role = UserRole.Student,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Email = "diana@student.com",
                PasswordHash = HashPassword("Student@123"),
                FullName = "Diana Prince",
                AvatarUrl = "https://api.dicebear.com/7.x/initials/svg?seed=DP",
                Bio = "Frontend developer looking to expand into backend.",
                Role = UserRole.Student,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Seeded {users.Count} users.");
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    private static async Task SeedCoursesAsync(AppDbContext context)
    {
        if (await context.Courses.AnyAsync()) return;

        var courses = new List<Course>
        {
            new()
            {
                Title = "ASP.NET Core 9 Complete Guide",
                Slug = "aspnet-core-9-complete-guide",
                Description = "Master ASP.NET Core 9 from scratch. Learn MVC, Web API, Entity Framework Core, authentication, and deployment to Azure.",
                ThumbnailUrl = "https://placehold.co/600x400/3b82f6/ffffff?text=ASP.NET+Core+9",
                Category = "Backend Development",
                Level = CourseLevel.Intermediate,
                IsPublished = true,
                TotalDurationMinutes = 0,
                TotalLessons = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Title = "React 18 & TypeScript Masterclass",
                Slug = "react-18-typescript-masterclass",
                Description = "Build modern, scalable React applications using TypeScript, hooks, context API, Redux Toolkit, and React Query.",
                ThumbnailUrl = "https://placehold.co/600x400/06b6d4/ffffff?text=React+18",
                Category = "Frontend Development",
                Level = CourseLevel.Intermediate,
                IsPublished = true,
                TotalDurationMinutes = 0,
                TotalLessons = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Title = "SQL Server & EF Core Deep Dive",
                Slug = "sql-server-ef-core-deep-dive",
                Description = "Deep dive into SQL Server, query optimization, and Entity Framework Core patterns including Code First, migrations, and advanced LINQ.",
                ThumbnailUrl = "https://placehold.co/600x400/f59e0b/ffffff?text=SQL+Server",
                Category = "Database",
                Level = CourseLevel.Advanced,
                IsPublished = true,
                TotalDurationMinutes = 0,
                TotalLessons = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Title = "Python for Beginners",
                Slug = "python-for-beginners",
                Description = "Start your programming journey with Python. Covers variables, loops, functions, OOP, file handling, and basic data science.",
                ThumbnailUrl = "https://placehold.co/600x400/10b981/ffffff?text=Python",
                Category = "Programming",
                Level = CourseLevel.Beginner,
                IsPublished = true,
                TotalDurationMinutes = 0,
                TotalLessons = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Title = "Docker & Kubernetes for Developers",
                Slug = "docker-kubernetes-for-developers",
                Description = "Learn containerization with Docker and orchestration with Kubernetes. Deploy production-ready microservices.",
                ThumbnailUrl = "https://placehold.co/600x400/6366f1/ffffff?text=Docker+K8s",
                Category = "DevOps",
                Level = CourseLevel.Advanced,
                IsPublished = false,
                TotalDurationMinutes = 0,
                TotalLessons = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Seeded {courses.Count} courses.");
    }

    // ── Lessons ───────────────────────────────────────────────────────────────

    private static async Task SeedLessonsAsync(AppDbContext context)
    {
        if (await context.Lessons.AnyAsync()) return;

        var courses = await context.Courses.ToListAsync();
        var aspnetCourse  = courses.First(c => c.Slug == "aspnet-core-9-complete-guide");
        var reactCourse   = courses.First(c => c.Slug == "react-18-typescript-masterclass");
        var sqlCourse     = courses.First(c => c.Slug == "sql-server-ef-core-deep-dive");
        var pythonCourse  = courses.First(c => c.Slug == "python-for-beginners");
        var dockerCourse  = courses.First(c => c.Slug == "docker-kubernetes-for-developers");

        var lessons = new List<Lesson>
        {
            // ASP.NET Core Course
            new() { CourseId = aspnetCourse.Id, Title = "Introduction to ASP.NET Core 9",          Description = "Overview of the framework and what we'll build.",           Content = "# Introduction\nASP.NET Core 9 is a cross-platform framework...", DurationMinutes = 12, OrderIndex = 1, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Setting Up Your Development Environment", Description = "Install .NET 9 SDK, VS 2022, and SQL Server.",               Content = "# Setup\n## Install .NET 9 SDK\n...",                           DurationMinutes = 18, OrderIndex = 2, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Understanding Middleware Pipeline",        Description = "Learn how requests flow through the middleware.",            Content = "# Middleware\nMiddleware are components assembled...",           DurationMinutes = 25, OrderIndex = 3, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Dependency Injection Deep Dive",          Description = "Transient, Scoped, and Singleton lifetimes explained.",      Content = "# DI in ASP.NET Core\n...",                                    DurationMinutes = 30, OrderIndex = 4, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Building RESTful APIs with Controllers",  Description = "Create CRUD endpoints using ApiController.",                Content = "# REST APIs\n...",                                             DurationMinutes = 40, OrderIndex = 5, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Entity Framework Core & Code First",      Description = "DbContext, migrations, and relationships.",                 Content = "# EF Core\n...",                                               DurationMinutes = 45, OrderIndex = 6, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Authentication with JWT",                 Description = "Secure your API with JSON Web Tokens.",                    Content = "# JWT Auth\n...",                                              DurationMinutes = 35, OrderIndex = 7, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = aspnetCourse.Id, Title = "Deploying to Azure App Service",          Description = "Publish your API to Azure with CI/CD pipelines.",           Content = "# Azure Deployment\n...",                                     DurationMinutes = 28, OrderIndex = 8, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // React Course
            new() { CourseId = reactCourse.Id, Title = "React 18 & TypeScript Setup",             Description = "Vite, ESLint, Prettier, and project structure.",            Content = "# React + TypeScript\n...",                                   DurationMinutes = 15, OrderIndex = 1, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = reactCourse.Id, Title = "Components, Props & State",                Description = "Functional components and useState hook.",                  Content = "# Components\n...",                                            DurationMinutes = 22, OrderIndex = 2, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = reactCourse.Id, Title = "useEffect & Data Fetching",                Description = "Side effects, cleanup, and fetching from APIs.",            Content = "# useEffect\n...",                                            DurationMinutes = 28, OrderIndex = 3, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = reactCourse.Id, Title = "Context API & Global State",               Description = "Share state across the component tree.",                    Content = "# Context API\n...",                                           DurationMinutes = 25, OrderIndex = 4, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = reactCourse.Id, Title = "Redux Toolkit",                            Description = "Scalable state management with RTK.",                       Content = "# Redux Toolkit\n...",                                         DurationMinutes = 35, OrderIndex = 5, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = reactCourse.Id, Title = "React Query for Server State",             Description = "Caching, background refetching, and mutations.",            Content = "# React Query\n...",                                           DurationMinutes = 30, OrderIndex = 6, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // SQL Course
            new() { CourseId = sqlCourse.Id, Title = "SQL Server Fundamentals",                   Description = "Tables, data types, constraints, and basic queries.",       Content = "# SQL Server\n...",                                           DurationMinutes = 20, OrderIndex = 1, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = sqlCourse.Id, Title = "Advanced Joins & Subqueries",               Description = "INNER, LEFT, RIGHT, FULL joins and nested queries.",        Content = "# Joins\n...",                                                DurationMinutes = 30, OrderIndex = 2, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = sqlCourse.Id, Title = "Indexes & Query Optimization",              Description = "Clustered, non-clustered indexes and execution plans.",     Content = "# Indexes\n...",                                              DurationMinutes = 40, OrderIndex = 3, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = sqlCourse.Id, Title = "EF Core Code First Migrations",             Description = "DbContext config, fluent API, and migration commands.",     Content = "# Migrations\n...",                                           DurationMinutes = 35, OrderIndex = 4, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = sqlCourse.Id, Title = "Repository Pattern with EF Core",           Description = "Implementing the repository and unit of work patterns.",    Content = "# Repository Pattern\n...",                                   DurationMinutes = 38, OrderIndex = 5, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // Python Course
            new() { CourseId = pythonCourse.Id, Title = "Welcome to Python",                      Description = "What is Python and why learn it?",                          Content = "# Welcome\nPython is a versatile language...",                DurationMinutes = 10, OrderIndex = 1, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = pythonCourse.Id, Title = "Variables & Data Types",                 Description = "Strings, integers, floats, booleans.",                      Content = "# Variables\n...",                                            DurationMinutes = 15, OrderIndex = 2, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = pythonCourse.Id, Title = "Control Flow: if, for, while",           Description = "Conditional logic and loops in Python.",                    Content = "# Control Flow\n...",                                         DurationMinutes = 20, OrderIndex = 3, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = pythonCourse.Id, Title = "Functions & Modules",                    Description = "Define functions, use built-in modules.",                   Content = "# Functions\n...",                                            DurationMinutes = 22, OrderIndex = 4, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = pythonCourse.Id, Title = "Object-Oriented Programming",            Description = "Classes, inheritance, and encapsulation.",                  Content = "# OOP\n...",                                                  DurationMinutes = 28, OrderIndex = 5, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

            // Docker Course
            new() { CourseId = dockerCourse.Id, Title = "Introduction to Containers",             Description = "What are containers and why Docker?",                       Content = "# Containers\n...",                                           DurationMinutes = 14, OrderIndex = 1, IsFree = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = dockerCourse.Id, Title = "Docker Images & Dockerfile",             Description = "Build custom images with Dockerfile.",                      Content = "# Dockerfile\n...",                                           DurationMinutes = 25, OrderIndex = 2, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = dockerCourse.Id, Title = "Docker Compose",                         Description = "Multi-container apps with docker-compose.yml.",             Content = "# Docker Compose\n...",                                       DurationMinutes = 30, OrderIndex = 3, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { CourseId = dockerCourse.Id, Title = "Kubernetes Fundamentals",                Description = "Pods, deployments, services, and namespaces.",              Content = "# Kubernetes\n...",                                           DurationMinutes = 40, OrderIndex = 4, IsFree = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        await context.Lessons.AddRangeAsync(lessons);
        await context.SaveChangesAsync();

        // Sync course totals
        foreach (var course in courses)
        {
            var courseLessons = lessons.Where(l => l.CourseId == course.Id).ToList();
            course.TotalLessons = courseLessons.Count;
            course.TotalDurationMinutes = courseLessons.Sum(l => l.DurationMinutes);
        }
        await context.SaveChangesAsync();

        Console.WriteLine($"[Seeder] Seeded {lessons.Count} lessons.");
    }

    // ── Enrollments ───────────────────────────────────────────────────────────

    private static async Task SeedEnrollmentsAsync(AppDbContext context)
    {
        if (await context.Enrollments.AnyAsync()) return;

        var users   = await context.Users.ToListAsync();
        var courses = await context.Courses.ToListAsync();

        var alice   = users.First(u => u.Email == "alice@student.com");
        var bob     = users.First(u => u.Email == "bob@student.com");
        var charlie = users.First(u => u.Email == "charlie@student.com");
        var diana   = users.First(u => u.Email == "diana@student.com");

        var aspnet  = courses.First(c => c.Slug == "aspnet-core-9-complete-guide");
        var react   = courses.First(c => c.Slug == "react-18-typescript-masterclass");
        var sql     = courses.First(c => c.Slug == "sql-server-ef-core-deep-dive");
        var python  = courses.First(c => c.Slug == "python-for-beginners");

        var enrollments = new List<Enrollment>
        {
            new() { UserId = alice.Id,   CourseId = aspnet.Id,  EnrolledAt = DateTime.UtcNow.AddDays(-30), ProgressPercentage = 75,  LastAccessedAt = DateTime.UtcNow.AddDays(-1) },
            new() { UserId = alice.Id,   CourseId = react.Id,   EnrolledAt = DateTime.UtcNow.AddDays(-20), ProgressPercentage = 40,  LastAccessedAt = DateTime.UtcNow.AddDays(-2) },
            new() { UserId = alice.Id,   CourseId = python.Id,  EnrolledAt = DateTime.UtcNow.AddDays(-10), ProgressPercentage = 100, CompletedAt = DateTime.UtcNow.AddDays(-3), LastAccessedAt = DateTime.UtcNow.AddDays(-3) },
            new() { UserId = bob.Id,     CourseId = aspnet.Id,  EnrolledAt = DateTime.UtcNow.AddDays(-15), ProgressPercentage = 25,  LastAccessedAt = DateTime.UtcNow.AddDays(-5) },
            new() { UserId = bob.Id,     CourseId = sql.Id,     EnrolledAt = DateTime.UtcNow.AddDays(-25), ProgressPercentage = 60,  LastAccessedAt = DateTime.UtcNow.AddDays(-1) },
            new() { UserId = charlie.Id, CourseId = python.Id,  EnrolledAt = DateTime.UtcNow.AddDays(-5),  ProgressPercentage = 20,  LastAccessedAt = DateTime.UtcNow.AddDays(-1) },
            new() { UserId = charlie.Id, CourseId = react.Id,   EnrolledAt = DateTime.UtcNow.AddDays(-8),  ProgressPercentage = 10,  LastAccessedAt = DateTime.UtcNow.AddDays(-4) },
            new() { UserId = diana.Id,   CourseId = react.Id,   EnrolledAt = DateTime.UtcNow.AddDays(-12), ProgressPercentage = 90,  LastAccessedAt = DateTime.UtcNow },
            new() { UserId = diana.Id,   CourseId = aspnet.Id,  EnrolledAt = DateTime.UtcNow.AddDays(-7),  ProgressPercentage = 15,  LastAccessedAt = DateTime.UtcNow.AddDays(-2) },
        };

        await context.Enrollments.AddRangeAsync(enrollments);
        await context.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Seeded {enrollments.Count} enrollments.");
    }

    // ── UserLessonProgress ────────────────────────────────────────────────────

    private static async Task SeedProgressAsync(AppDbContext context)
    {
        if (await context.UserLessonProgresses.AnyAsync()) return;

        var users   = await context.Users.ToListAsync();
        var lessons = await context.Lessons.ToListAsync();

        var alice  = users.First(u => u.Email == "alice@student.com");
        var bob    = users.First(u => u.Email == "bob@student.com");
        var diana  = users.First(u => u.Email == "diana@student.com");

        var aspnetLessons = lessons.Where(l => l.OrderIndex <= 6)
                                   .OrderBy(l => l.OrderIndex).ToList();
        var reactLessons  = lessons.Where(l => l.OrderIndex <= 4)
                                   .OrderBy(l => l.OrderIndex).ToList();

        var progresses = new List<UserLessonProgress>();

        // Alice — completed first 6 ASP.NET lessons
        foreach (var lesson in aspnetLessons.Take(6))
        {
            progresses.Add(new()
            {
                UserId = alice.Id,
                LessonId = lesson.Id,
                LastScrollPercentage = 100,
                UnlockedCheckpointIndex = 3,
                Completed = true,
                CompletedAt = DateTime.UtcNow.AddDays(-2),
                LastPositionSeconds = lesson.DurationMinutes * 60,
                WatchTimeSeconds = lesson.DurationMinutes * 60,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }

        // Alice — in progress on React lessons 1–3
        foreach (var (lesson, idx) in reactLessons.Take(3).Select((l, i) => (l, i)))
        {
            progresses.Add(new()
            {
                UserId = alice.Id,
                LessonId = lesson.Id,
                LastScrollPercentage = idx < 2 ? 100 : 55,
                UnlockedCheckpointIndex = idx < 2 ? 3 : 1,
                Completed = idx < 2,
                CompletedAt = idx < 2 ? DateTime.UtcNow.AddDays(-5) : null,
                LastPositionSeconds = idx < 2 ? lesson.DurationMinutes * 60 : lesson.DurationMinutes * 33,
                WatchTimeSeconds = idx < 2 ? lesson.DurationMinutes * 60 : lesson.DurationMinutes * 33,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }

        // Bob — first 2 ASP.NET lessons completed
        foreach (var (lesson, idx) in aspnetLessons.Take(2).Select((l, i) => (l, i)))
        {
            progresses.Add(new()
            {
                UserId = bob.Id,
                LessonId = lesson.Id,
                LastScrollPercentage = 100,
                UnlockedCheckpointIndex = 3,
                Completed = true,
                CompletedAt = DateTime.UtcNow.AddDays(-10),
                LastPositionSeconds = lesson.DurationMinutes * 60,
                WatchTimeSeconds = lesson.DurationMinutes * 60,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            });
        }

        // Diana — React lessons 1–4 completed, 5 in progress
        foreach (var (lesson, idx) in reactLessons.Take(5).Select((l, i) => (l, i)))
        {
            progresses.Add(new()
            {
                UserId = diana.Id,
                LessonId = lesson.Id,
                LastScrollPercentage = idx < 4 ? 100 : 70,
                UnlockedCheckpointIndex = idx < 4 ? 3 : 2,
                Completed = idx < 4,
                CompletedAt = idx < 4 ? DateTime.UtcNow.AddDays(-3) : null,
                LastPositionSeconds = idx < 4 ? lesson.DurationMinutes * 60 : lesson.DurationMinutes * 42,
                WatchTimeSeconds = idx < 4 ? lesson.DurationMinutes * 60 : lesson.DurationMinutes * 42,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        await context.UserLessonProgresses.AddRangeAsync(progresses);
        await context.SaveChangesAsync();
        Console.WriteLine($"[Seeder] Seeded {progresses.Count} progress records.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string HashPassword(string password)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"hashed_{password}"));
}
