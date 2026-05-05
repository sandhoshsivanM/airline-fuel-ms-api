using System.Text;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Render (and most PaaS) sets the PORT env var. Bind to it so the container is reachable.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// — Database
//   Local dev:  uses ConnectionStrings:DefaultConnection (SQLite file).
//   Production: if DATABASE_URL env var is set (e.g. on Render), connect to Postgres.
//               Render gives a postgres:// URI which we convert to Npgsql keyword=value.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var usePostgres = !string.IsNullOrWhiteSpace(databaseUrl);

builder.Services.AddDbContext<AppDbContext>(opts =>
{
    if (usePostgres)
    {
        var uri = new Uri(databaseUrl!);
        var userInfo = uri.UserInfo.Split(':', 2);
        var pgConnStr =
            $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
            $"Database={uri.AbsolutePath.TrimStart('/')};" +
            $"Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};" +
            $"SSL Mode=Require;Trust Server Certificate=true";
        opts.UseNpgsql(pgConnStr);
    }
    else
    {
        opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// — Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAirlineService, AirlineService>();
builder.Services.AddScoped<IFuelProviderService, FuelProviderService>();
builder.Services.AddScoped<IFuelTransactionService, FuelTransactionService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// — JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// — Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Airline Fuel MS", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your-token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// — CORS (adjust origins for production)
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// — Seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (usePostgres)
    {
        // Postgres: schema is provider-specific; bypass SQLite migrations and create from model.
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
    await SeedData.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
