
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MisaTracker.Core;
using MisaTracker.Core.ORM.DataBase;
using MisaTrackerr.Server.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace MisaTrackerr.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("https://misatracker-1.onrender.com")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            // SQL Server
            var connectionString =
                builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Не найдена строка подключения DefaultConnection в appsettings.json");
            }

            builder.Services.AddDbContextFactory<DbConnection>(options =>
                options.UseSqlServer(connectionString));

            // Сервисы приложения
            builder.Services.AddScoped<DbCrud>();
            builder.Services.AddScoped<UserInfrastructure>();
            builder.Services.AddScoped<PushNotificationService>();
            builder.Services.AddHostedService<TaskReminderBackgroundService>();
            // Защита от большого количества запросов
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter =
                    PartitionedRateLimiter.Create<HttpContext, string>(
                        httpContext =>
                        {
                            var ip = httpContext.Connection
                                .RemoteIpAddress?.ToString() ?? "unknown";

                            return RateLimitPartition.GetFixedWindowLimiter(
                                ip,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 100,
                                    Window = TimeSpan.FromMinutes(1),
                                    QueueLimit = 0,
                                    AutoReplenishment = true
                                });
                        });
            });

            // JWT configuration
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key не настроен в appsettings.json");

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer не настроен в appsettings.json");

            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience не настроен в appsettings.json");

            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT Key должен содержать минимум 32 байта.");
            }

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = jwtIssuer,

                            ValidateAudience = true,
                            ValidAudience = jwtAudience,

                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtKey)),

                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(30)
                        };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Создание базы, если её ещё нет
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<DbConnection>>();

                await using var db =
                    await factory.CreateDbContextAsync();

                await db.Database.EnsureCreatedAsync();
            }

            app.UseDefaultFiles();
            app.MapStaticAssets();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers();
            app.MapFallbackToFile("/index.html");

            await app.RunAsync();
        }
    }
}
