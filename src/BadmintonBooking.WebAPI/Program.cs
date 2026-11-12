using System.Text;
using BadmintonBooking.BLL.Data;
using BadmintonBooking.BLL.Mappings;
using BadmintonBooking.BLL.Services.Implementations;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL;
using BadmintonBooking.DAL.Repositories;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.WebAPI.Formatters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
    }));

// 2. Unit of Work & Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 3. Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IVietQrService, VietQrService>();
builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHostedService<BadmintonBooking.WebAPI.BackgroundJobs.PaymentExpiryService>();

// 4. AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// 5. Controllers with Content Negotiation (JSON, XML, CSV) and OData
builder.Services.AddControllers(options =>
{
    // Custom CSV Formatter
    options.OutputFormatters.Add(new CsvOutputFormatter());
})
.AddXmlSerializerFormatters()
.AddOData(options => options
    .Select()
    .Filter()
    .OrderBy()
    .Expand()
    .Count()
    .SetMaxTop(100));

// 6. JWT Bearer Authentication & Authorization
// Khóa ký JWT bắt buộc phải cấu hình (không có giá trị mặc định trong code).
// Đặt qua biến môi trường: Jwt__Key=<chuỗi bí mật >= 32 ký tự>
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Chưa cấu hình Jwt:Key (tối thiểu 32 ký tự). Đặt biến môi trường Jwt__Key.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BadmintonBookingAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BadmintonBookingClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    // Đọc JWT từ cookie HttpOnly (WebClient) nếu không có header Authorization
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token)
                && context.Request.Cookies.TryGetValue(BadmintonBooking.WebAPI.Controllers.AuthController.AuthCookieName, out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 7. CORS
builder.Services.AddCors(options =>
{
    // Chỉ cho phép WebClient gọi API (cấu hình Cors:AllowedOrigins). Xác thực bằng Bearer token, không dùng cookie.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:3456" };
    options.AddPolicy("AllowWebClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cookie xác thực
    });
});

// Rate limit: chống dò mật khẩu và spam đăng nhập (theo IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Giới hạn chung cho mọi API theo IP (300 request/phút). Lưu trong bộ nhớ: phù hợp một instance.
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// 8. Swagger / OpenAPI with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Badminton Court Booking API",
        Version = "v1",
        Description = "Centralized ASP.NET Core Web API cho hệ thống đặt sân cầu lông đa nhà cung cấp (Multi-Provider Badminton Court Booking System)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Ví dụ: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 9. Database Auto-Migration & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Initializing Database and Applying Migrations / Seeding...");
        await DbInitializer.SeedAsync(context, builder.Configuration);
        logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not initialize/migrate database on startup (SQL Server might be offline or initializing).");
    }
}

// Configure the HTTP request pipeline
// Swagger chỉ bật khi môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Badminton Booking API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors("AllowWebClient");
app.UseStaticFiles();
app.UseRateLimiter();

// Chống CSRF: mọi request thay đổi dữ liệu phải kèm header X-Requested-With: CourtSideWeb.
// Form HTML thông thường (cross-site) không thể gắn header này; CORS đã chặn origin lạ.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    // Webhook SePay không gửi header CSRF; endpoint này tự xác thực bằng API key
    var isSepayWebhook = context.Request.Path.Equals(BadmintonBooking.WebAPI.Controllers.PaymentsController.SepayWebhookPath, StringComparison.OrdinalIgnoreCase);
    if (!isSepayWebhook && (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method)))
    {
        if (!context.Request.Headers.TryGetValue("X-Requested-With", out var marker) || marker != "CourtSideWeb")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Yêu cầu không hợp lệ (thiếu header bảo vệ CSRF)." });
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
