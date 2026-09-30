using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OverLoad.API.Common;
using OverLoad.API.Infrastructure;
using OverLoad.API.Middleware;
using OverLoad.API.Validators;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Implementations;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;
using PayOS;

// Local runs: pick up DATABASE_URL and other secrets from the repo's .env (never shipped in the image).
DatabaseConnection.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// ── Settings (Options pattern) ────────────────────────────────────────────────
// Values come from appsettings.json, user-secrets or environment variables (e.g. Jwt__SecretKey).
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection(GeminiSettings.SectionName));
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection(PayOSSettings.SectionName));
builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection(PaymentSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
    throw new InvalidOperationException("Jwt:SecretKey is not configured.");

// ── Database ─────────────────────────────────────────────────────────────────
// DATABASE_URL (postgresql://...) or ConnectionStrings:DefaultConnection — see DatabaseConnection.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        DatabaseConnection.Resolve(builder.Configuration),
        npgsql => npgsql.MigrationsAssembly("OverLoad.Repositories")));

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IUserLessonProgressRepository, UserLessonProgressRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseAccessService, CourseAccessService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPaymentGateway, PayOSGateway>();

// HttpClient cho Gemini
builder.Services.AddHttpClient<IChatService, GeminiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── PayOS Client ─────────────────────────────────────────────────────────────
// PAYOS_* environment variables are still honoured as a fallback for existing deployments.
var payOs = builder.Configuration.GetSection(PayOSSettings.SectionName).Get<PayOSSettings>() ?? new PayOSSettings();
builder.Services.AddSingleton(new PayOSClient(
    NonEmptyOr(payOs.ClientId, Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID")),
    NonEmptyOr(payOs.ApiKey, Environment.GetEnvironmentVariable("PAYOS_API_KEY")),
    NonEmptyOr(payOs.ChecksumKey, Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY"))));

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // 401/403 produced by [Authorize] use the same ApiResponse envelope as the rest of the API.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            await WriteEnvelopeAsync(context.Response, StatusCodes.Status401Unauthorized,
                "Authentication is required to access this resource.");
        },
        OnForbidden = context => WriteEnvelopeAsync(context.Response, StatusCodes.Status403Forbidden,
            "You do not have permission to access this resource.")
    };
});

builder.Services.AddAuthorization();

// ── Controllers ───────────────────────────────────────────────────────────────
// Enums travel over JSON as their names ("Beginner"), not their numeric values.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model-binding and FluentValidation failures come back in the ApiResponse envelope
        // instead of the framework's ProblemDetails shape.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(entry.Key)
                        ? error.ErrorMessage
                        : $"{entry.Key}: {error.ErrorMessage}"))
                .ToList();

            return new BadRequestObjectResult(ApiResponse.Fail("Validation failed.", errors));
        };
    });

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestModelValidator>();

// ── API versioning: /api/v1/... ───────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OverLoad API",
        Version = "v1",
        Description = "RESTful API for the OverLoad e-learning platform",
        Contact = new OpenApiContact
        {
            Name = "OverLoad Team",
            Email = "dev@overload.io"
        }
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token của bạn vào đây."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if present
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── CORS ──────────────────────────────────────────────────────────────────────
// Browsers may only call the API from the origins listed in Cors:AllowedOrigins
// (e.g. Cors__AllowedOrigins__0=https://overload.vn). With none configured, Development allows
// any origin so local frontends work; any other environment allows none.
const string CorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
    else if (builder.Environment.IsDevelopment())
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger is on in Development; elsewhere (staging, a demo server) opt in with Swagger__Enabled=true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OverLoad API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
        c.DocumentTitle = "OverLoad API";
        c.DefaultModelsExpandDepth(-1); // Hide schema section by default
        c.EnablePersistAuthorization();
    });
}

app.UseCors(CorsPolicy);
// Off in the container image, where TLS is terminated by the hosting proxy (HttpsRedirection__Enabled=false).
if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check for CI/CD smoke tests and load balancers
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
// On by default in Development only. Production migrations are run deliberately
// (dotnet ef database update) after a backup — never implicitly on deploy.
if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

static string NonEmptyOr(string value, string? fallback) =>
    string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;

static Task WriteEnvelopeAsync(HttpResponse response, int statusCode, string message)
{
    response.StatusCode = statusCode;
    response.ContentType = "application/json";
    return response.WriteAsync(JsonSerializer.Serialize(
        ApiResponse.Fail(message),
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}

// Exposed so the integration tests can reference the entry point through WebApplicationFactory.
public partial class Program { }
