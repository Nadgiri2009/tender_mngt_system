using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TenderManagement.API.Middleware;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;
using TenderManagement.Infrastructure.Data;
using TenderManagement.Infrastructure.Repositories;
using TenderManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration Debug Validation
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' is missing. " +
        "Check appsettings.json or appsettings.Development.json.");
}


// ---------------------------------------------------------------------------
// Controllers + JSON
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


// ---------------------------------------------------------------------------
// Swagger / OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "Tender Management System API",
            Version = "v1"
        });

    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using Bearer scheme. Example: Bearer {token}",

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
                Reference =
                    new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
            },
            Array.Empty<string>()
        }
    });
});


// ---------------------------------------------------------------------------
// EF Core / SQL Server
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});


// ---------------------------------------------------------------------------
// Dependency Injection
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddScoped<IFileStorageService>(sp =>
    new FileStorageService(
        sp.GetRequiredService<IWebHostEnvironment>().WebRootPath
        ?? Directory.GetCurrentDirectory()
    ));


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IWorkService, WorkService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();


// ---------------------------------------------------------------------------
// JWT Authentication
// ---------------------------------------------------------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short. Minimum 32 characters required.");
}


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

            ClockSkew = TimeSpan.FromMinutes(2)
        };
});


builder.Services.AddAuthorization();


// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
var allowedOrigins =
    builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ??
    new[]
    {
        "http://localhost:3000"
    };


builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "DefaultCorsPolicy",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


// ---------------------------------------------------------------------------
// Build Application
// ---------------------------------------------------------------------------
var app = builder.Build();


// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();


app.Run();