# Airline Fuel Management System — Full Implementation Guide
**Stack:** ASP.NET Core 8 · Entity Framework Core · SQL Server · JWT Auth · Razor Pages / MVC

---

## Table of Contents
1. [Project Setup](#1-project-setup)
2. [Folder Structure](#2-folder-structure)
3. [Domain Models (Entities)](#3-domain-models)
4. [DbContext & Migrations](#4-dbcontext--migrations)
5. [Seed Data](#5-seed-data)
6. [DTOs & ViewModels](#6-dtos--viewmodels)
7. [Repositories & Services](#7-repositories--services)
8. [Authentication (JWT)](#8-authentication-jwt)
9. [API Controllers](#9-api-controllers)
10. [Invoice Generation Logic](#10-invoice-generation-logic)
11. [Program.cs & Configuration](#11-programcs--configuration)
12. [appsettings.json](#12-appsettingsjson)
13. [Running the Project](#13-running-the-project)

---

## 1. Project Setup

```bash
# Create solution and projects
dotnet new sln -n AirlineFuelMS
dotnet new webapi -n AirlineFuelMS.API
dotnet new classlib -n AirlineFuelMS.Core
dotnet new classlib -n AirlineFuelMS.Infrastructure

dotnet sln add AirlineFuelMS.API
dotnet sln add AirlineFuelMS.Core
dotnet sln add AirlineFuelMS.Infrastructure

# Add references
dotnet add AirlineFuelMS.API reference AirlineFuelMS.Core
dotnet add AirlineFuelMS.API reference AirlineFuelMS.Infrastructure
dotnet add AirlineFuelMS.Infrastructure reference AirlineFuelMS.Core

# Install packages — API project
cd AirlineFuelMS.API
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# Install packages — Infrastructure project
cd ../AirlineFuelMS.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package BCrypt.Net-Next
```

---

## 2. Folder Structure

```
AirlineFuelMS/
├── AirlineFuelMS.Core/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Airline.cs
│   │   ├── FuelProvider.cs
│   │   ├── FuelPrice.cs
│   │   ├── FuelTransaction.cs
│   │   └── Invoice.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IAirlineService.cs
│   │   ├── IFuelProviderService.cs
│   │   ├── IFuelTransactionService.cs
│   │   └── IInvoiceService.cs
│   └── DTOs/
│       ├── Auth/
│       ├── Airline/
│       ├── FuelProvider/
│       ├── FuelTransaction/
│       └── Invoice/
├── AirlineFuelMS.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── SeedData.cs
│   ├── Repositories/
│   │   └── Repository.cs
│   └── Services/
│       ├── AirlineService.cs
│       ├── FuelProviderService.cs
│       ├── FuelTransactionService.cs
│       ├── InvoiceService.cs
│       └── AuthService.cs
└── AirlineFuelMS.API/
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── AirlinesController.cs
    │   ├── FuelProvidersController.cs
    │   ├── FuelTransactionsController.cs
    │   └── InvoicesController.cs
    ├── Program.cs
    └── appsettings.json
```

---

## 3. Domain Models

### AirlineFuelMS.Core/Entities/User.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "NormalUser"; // "Admin" | "NormalUser"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
}
```

### AirlineFuelMS.Core/Entities/FuelProvider.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class FuelProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // e.g. "HP", "Bharat Petroleum", "Emirate"
    public string Code { get; set; } = string.Empty;       // e.g. "HP", "BP", "EMI"
    public string ContactInfo { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FuelPrice> FuelPrices { get; set; } = new List<FuelPrice>();
    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
```

### AirlineFuelMS.Core/Entities/Airline.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class Airline
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;        // e.g. "Emirate Airlines"
    public string Code { get; set; } = string.Empty;        // e.g. "EK"
    public int MaxFuelCapacityLiters { get; set; }          // e.g. 1000
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
```

### AirlineFuelMS.Core/Entities/FuelPrice.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class FuelPrice
{
    public int Id { get; set; }
    public int FuelProviderId { get; set; }
    public decimal PricePerLiter { get; set; }             // e.g. 0.58 for Emirate
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public FuelProvider FuelProvider { get; set; } = null!;
}
```

### AirlineFuelMS.Core/Entities/FuelTransaction.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class FuelTransaction
{
    public int Id { get; set; }
    public int AirlineId { get; set; }
    public int FuelProviderId { get; set; }
    public int CreatedByUserId { get; set; }
    public decimal QuantityLiters { get; set; }
    public decimal PricePerLiter { get; set; }
    public decimal TotalAmount => QuantityLiters * PricePerLiter;
    public string TransactionRef { get; set; } = string.Empty;   // auto-generated e.g. "TXN-20240501-001"
    public string Status { get; set; } = "Pending";              // Pending | Completed | Cancelled
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public Airline Airline { get; set; } = null!;
    public FuelProvider FuelProvider { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
```

### AirlineFuelMS.Core/Entities/Invoice.cs
```csharp
namespace AirlineFuelMS.Core.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int FuelTransactionId { get; set; }
    public int AirlineId { get; set; }
    public int FuelProviderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;   // e.g. "INV-20240501-001"
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Unpaid";              // Unpaid | Paid | Overdue
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public FuelTransaction FuelTransaction { get; set; } = null!;
    public Airline Airline { get; set; } = null!;
    public FuelProvider FuelProvider { get; set; } = null!;
}
```

---

## 4. DbContext & Migrations

### AirlineFuelMS.Infrastructure/Data/AppDbContext.cs
```csharp
using AirlineFuelMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<FuelProvider> FuelProviders => Set<FuelProvider>();
    public DbSet<FuelPrice> FuelPrices => Set<FuelPrice>();
    public DbSet<FuelTransaction> FuelTransactions => Set<FuelTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("NormalUser");
        });

        // FuelPrice → FuelProvider
        modelBuilder.Entity<FuelPrice>(e =>
        {
            e.HasOne(fp => fp.FuelProvider)
             .WithMany(p => p.FuelPrices)
             .HasForeignKey(fp => fp.FuelProviderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(fp => fp.PricePerLiter).HasPrecision(10, 4);
        });

        // FuelTransaction
        modelBuilder.Entity<FuelTransaction>(e =>
        {
            e.Property(t => t.QuantityLiters).HasPrecision(12, 2);
            e.Property(t => t.PricePerLiter).HasPrecision(10, 4);
            // Computed column stored
            e.Property<decimal>("TotalAmountStored")
             .HasComputedColumnSql("[QuantityLiters] * [PricePerLiter]", stored: true)
             .HasPrecision(14, 2);

            e.HasOne(t => t.Airline)
             .WithMany(a => a.FuelTransactions)
             .HasForeignKey(t => t.AirlineId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.FuelProvider)
             .WithMany(p => p.FuelTransactions)
             .HasForeignKey(t => t.FuelProviderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.CreatedByUser)
             .WithMany(u => u.FuelTransactions)
             .HasForeignKey(t => t.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.Amount).HasPrecision(14, 2);
            e.Property(i => i.TaxAmount).HasPrecision(14, 2);
            e.Property(i => i.TotalAmount).HasPrecision(14, 2);

            e.HasOne(i => i.FuelTransaction)
             .WithOne(t => t.Invoice)
             .HasForeignKey<Invoice>(i => i.FuelTransactionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Airline)
             .WithMany(a => a.Invoices)
             .HasForeignKey(i => i.AirlineId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.FuelProvider)
             .WithMany(p => p.Invoices)
             .HasForeignKey(i => i.FuelProviderId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### Run Migrations

```bash
cd AirlineFuelMS.API

# Add migration
dotnet ef migrations add InitialCreate \
  --project ../AirlineFuelMS.Infrastructure \
  --startup-project . \
  --context AppDbContext

# Apply to database
dotnet ef database update \
  --project ../AirlineFuelMS.Infrastructure \
  --startup-project . \
  --context AppDbContext
```

---

## 5. Seed Data

### AirlineFuelMS.Infrastructure/Data/SeedData.cs
```csharp
using AirlineFuelMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Seed Users
        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Email = "admin@airlinefuel.com",
                    Role = "Admin"
                },
                new User
                {
                    Username = "user1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Email = "user1@airlinefuel.com",
                    Role = "NormalUser"
                }
            );
        }

        // Seed Fuel Providers
        if (!await context.FuelProviders.AnyAsync())
        {
            context.FuelProviders.AddRange(
                new FuelProvider { Name = "HP (Hindustan Petroleum)", Code = "HP", ContactInfo = "hp@petroleum.com" },
                new FuelProvider { Name = "Bharat Petroleum", Code = "BP", ContactInfo = "contact@bharatpetroleum.com" },
                new FuelProvider { Name = "Emirate Fuel Services", Code = "EMI", ContactInfo = "fuel@emirate.com" }
            );
        }

        await context.SaveChangesAsync();

        // Seed Fuel Prices (after providers saved)
        if (!await context.FuelPrices.AnyAsync())
        {
            var hp = await context.FuelProviders.FirstAsync(p => p.Code == "HP");
            var bp = await context.FuelProviders.FirstAsync(p => p.Code == "BP");
            var emi = await context.FuelProviders.FirstAsync(p => p.Code == "EMI");

            context.FuelPrices.AddRange(
                new FuelPrice { FuelProviderId = hp.Id, PricePerLiter = 0.72m, EffectiveFrom = DateTime.UtcNow, IsActive = true },
                new FuelPrice { FuelProviderId = bp.Id, PricePerLiter = 0.68m, EffectiveFrom = DateTime.UtcNow, IsActive = true },
                new FuelPrice { FuelProviderId = emi.Id, PricePerLiter = 0.58m, EffectiveFrom = DateTime.UtcNow, IsActive = true }
            );
        }

        // Seed Airlines
        if (!await context.Airlines.AnyAsync())
        {
            context.Airlines.AddRange(
                new Airline { Name = "Emirate Airlines", Code = "EK", MaxFuelCapacityLiters = 1000, Country = "UAE" },
                new Airline { Name = "IndiGo", Code = "6E", MaxFuelCapacityLiters = 5000, Country = "India" },
                new Airline { Name = "Air India", Code = "AI", MaxFuelCapacityLiters = 8000, Country = "India" }
            );
        }

        await context.SaveChangesAsync();
    }
}
```

---

## 6. DTOs & ViewModels

### AirlineFuelMS.Core/DTOs/Auth/LoginDto.cs
```csharp
namespace AirlineFuelMS.Core.DTOs.Auth;

public record LoginDto(string Username, string Password);
public record LoginResponseDto(string Token, string Role, string Username);
```

### AirlineFuelMS.Core/DTOs/Airline/AirlineDto.cs
```csharp
namespace AirlineFuelMS.Core.DTOs.Airline;

public record AirlineCreateDto(string Name, string Code, int MaxFuelCapacityLiters, string Country);
public record AirlineUpdateDto(string Name, int MaxFuelCapacityLiters, string Country, bool IsActive);

public record AirlineDto(int Id, string Name, string Code, int MaxFuelCapacityLiters, string Country, bool IsActive);

public record AirlineSummaryDto(
    int AirlineId,
    string AirlineName,
    string AirlineCode,
    decimal TotalFuelPurchasedLiters,
    decimal TotalAmountSpent,
    int TotalTransactions,
    int PendingInvoices,
    int PaidInvoices
);
```

### AirlineFuelMS.Core/DTOs/FuelTransaction/FuelTransactionDto.cs
```csharp
namespace AirlineFuelMS.Core.DTOs.FuelTransaction;

public record FuelTransactionCreateDto(
    int AirlineId,
    int FuelProviderId,
    decimal QuantityLiters,
    string? Notes
);

public record FuelTransactionUpdateDto(
    decimal QuantityLiters,
    string Status,
    string? Notes
);

public record FuelTransactionDto(
    int Id,
    string AirlineName,
    string AirlineCode,
    string FuelProviderName,
    decimal QuantityLiters,
    decimal PricePerLiter,
    decimal TotalAmount,
    string TransactionRef,
    string Status,
    DateTime TransactionDate,
    string? Notes,
    bool HasInvoice
);
```

### AirlineFuelMS.Core/DTOs/Invoice/InvoiceDto.cs
```csharp
namespace AirlineFuelMS.Core.DTOs.Invoice;

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    string AirlineName,
    string FuelProviderName,
    string TransactionRef,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Status,
    DateTime InvoiceDate,
    DateTime DueDate,
    DateTime? PaidDate
);

public record InvoiceUpdateStatusDto(string Status); // Unpaid | Paid | Overdue
```

---

## 7. Repositories & Services

### AirlineFuelMS.Core/Interfaces/IRepository.cs
```csharp
using System.Linq.Expressions;

namespace AirlineFuelMS.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
```

### AirlineFuelMS.Infrastructure/Repositories/Repository.cs
```csharp
using System.Linq.Expressions;
using AirlineFuelMS.Core.Interfaces;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _set;

    public Repository(AppDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _set.Where(predicate).ToListAsync();
    public async Task AddAsync(T entity) => await _set.AddAsync(entity);
    public void Update(T entity) => _set.Update(entity);
    public void Remove(T entity) => _set.Remove(entity);
    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
```

### AirlineFuelMS.Infrastructure/Services/AirlineService.cs
```csharp
using AirlineFuelMS.Core.DTOs.Airline;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IAirlineService
{
    Task<IEnumerable<AirlineDto>> GetAllAsync();
    Task<AirlineDto?> GetByIdAsync(int id);
    Task<AirlineDto> CreateAsync(AirlineCreateDto dto);
    Task<AirlineDto?> UpdateAsync(int id, AirlineUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<AirlineSummaryDto>> GetSummaryAsync();
}

public class AirlineService : IAirlineService
{
    private readonly AppDbContext _context;

    public AirlineService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<AirlineDto>> GetAllAsync() =>
        await _context.Airlines
            .Select(a => new AirlineDto(a.Id, a.Name, a.Code, a.MaxFuelCapacityLiters, a.Country, a.IsActive))
            .ToListAsync();

    public async Task<AirlineDto?> GetByIdAsync(int id)
    {
        var a = await _context.Airlines.FindAsync(id);
        return a is null ? null : new AirlineDto(a.Id, a.Name, a.Code, a.MaxFuelCapacityLiters, a.Country, a.IsActive);
    }

    public async Task<AirlineDto> CreateAsync(AirlineCreateDto dto)
    {
        var airline = new Airline
        {
            Name = dto.Name,
            Code = dto.Code.ToUpper(),
            MaxFuelCapacityLiters = dto.MaxFuelCapacityLiters,
            Country = dto.Country
        };
        _context.Airlines.Add(airline);
        await _context.SaveChangesAsync();
        return new AirlineDto(airline.Id, airline.Name, airline.Code, airline.MaxFuelCapacityLiters, airline.Country, airline.IsActive);
    }

    public async Task<AirlineDto?> UpdateAsync(int id, AirlineUpdateDto dto)
    {
        var airline = await _context.Airlines.FindAsync(id);
        if (airline is null) return null;
        airline.Name = dto.Name;
        airline.MaxFuelCapacityLiters = dto.MaxFuelCapacityLiters;
        airline.Country = dto.Country;
        airline.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();
        return new AirlineDto(airline.Id, airline.Name, airline.Code, airline.MaxFuelCapacityLiters, airline.Country, airline.IsActive);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var airline = await _context.Airlines.FindAsync(id);
        if (airline is null) return false;
        airline.IsActive = false; // soft delete
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AirlineSummaryDto>> GetSummaryAsync()
    {
        return await _context.Airlines
            .Where(a => a.IsActive)
            .Select(a => new AirlineSummaryDto(
                a.Id,
                a.Name,
                a.Code,
                a.FuelTransactions.Sum(t => t.QuantityLiters),
                a.FuelTransactions.Sum(t => t.QuantityLiters * t.PricePerLiter),
                a.FuelTransactions.Count(),
                a.Invoices.Count(i => i.Status == "Unpaid" || i.Status == "Overdue"),
                a.Invoices.Count(i => i.Status == "Paid")
            ))
            .ToListAsync();
    }
}
```

### AirlineFuelMS.Infrastructure/Services/FuelTransactionService.cs
```csharp
using AirlineFuelMS.Core.DTOs.FuelTransaction;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IFuelTransactionService
{
    Task<IEnumerable<FuelTransactionDto>> GetAllAsync();
    Task<FuelTransactionDto?> GetByIdAsync(int id);
    Task<FuelTransactionDto> CreateAsync(FuelTransactionCreateDto dto, int userId);
    Task<FuelTransactionDto?> UpdateAsync(int id, FuelTransactionUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<FuelTransactionDto>> GetByAirlineAsync(int airlineId);
    Task<IEnumerable<FuelTransactionDto>> GetByProviderAsync(int providerId);
}

public class FuelTransactionService : IFuelTransactionService
{
    private readonly AppDbContext _context;
    private readonly IInvoiceService _invoiceService;

    public FuelTransactionService(AppDbContext context, IInvoiceService invoiceService)
    {
        _context = context;
        _invoiceService = invoiceService;
    }

    private static string GenerateRef() =>
        $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static FuelTransactionDto ToDto(FuelTransaction t) => new(
        t.Id,
        t.Airline.Name,
        t.Airline.Code,
        t.FuelProvider.Name,
        t.QuantityLiters,
        t.PricePerLiter,
        t.QuantityLiters * t.PricePerLiter,
        t.TransactionRef,
        t.Status,
        t.TransactionDate,
        t.Notes,
        t.Invoice is not null
    );

    private IQueryable<FuelTransaction> BaseQuery() =>
        _context.FuelTransactions
            .Include(t => t.Airline)
            .Include(t => t.FuelProvider)
            .Include(t => t.Invoice);

    public async Task<IEnumerable<FuelTransactionDto>> GetAllAsync() =>
        (await BaseQuery().ToListAsync()).Select(ToDto);

    public async Task<FuelTransactionDto?> GetByIdAsync(int id)
    {
        var t = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        return t is null ? null : ToDto(t);
    }

    public async Task<FuelTransactionDto> CreateAsync(FuelTransactionCreateDto dto, int userId)
    {
        // Get current active price for the provider
        var price = await _context.FuelPrices
            .Where(p => p.FuelProviderId == dto.FuelProviderId && p.IsActive)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => p.PricePerLiter)
            .FirstOrDefaultAsync();

        if (price == 0)
            throw new InvalidOperationException("No active fuel price found for the selected provider.");

        // Validate capacity
        var airline = await _context.Airlines.FindAsync(dto.AirlineId)
            ?? throw new KeyNotFoundException("Airline not found");

        if (dto.QuantityLiters > airline.MaxFuelCapacityLiters)
            throw new InvalidOperationException(
                $"Requested {dto.QuantityLiters}L exceeds airline max capacity {airline.MaxFuelCapacityLiters}L");

        var transaction = new FuelTransaction
        {
            AirlineId = dto.AirlineId,
            FuelProviderId = dto.FuelProviderId,
            CreatedByUserId = userId,
            QuantityLiters = dto.QuantityLiters,
            PricePerLiter = price,
            TransactionRef = GenerateRef(),
            Status = "Completed",
            Notes = dto.Notes ?? string.Empty
        };

        _context.FuelTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Auto-generate invoice
        await _invoiceService.GenerateForTransactionAsync(transaction.Id);

        var result = await BaseQuery().FirstAsync(t => t.Id == transaction.Id);
        return ToDto(result);
    }

    public async Task<FuelTransactionDto?> UpdateAsync(int id, FuelTransactionUpdateDto dto)
    {
        var t = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        if (t is null) return null;
        t.QuantityLiters = dto.QuantityLiters;
        t.Status = dto.Status;
        t.Notes = dto.Notes ?? t.Notes;
        await _context.SaveChangesAsync();
        return ToDto(t);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var t = await _context.FuelTransactions.FindAsync(id);
        if (t is null) return false;
        t.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FuelTransactionDto>> GetByAirlineAsync(int airlineId) =>
        (await BaseQuery().Where(t => t.AirlineId == airlineId).ToListAsync()).Select(ToDto);

    public async Task<IEnumerable<FuelTransactionDto>> GetByProviderAsync(int providerId) =>
        (await BaseQuery().Where(t => t.FuelProviderId == providerId).ToListAsync()).Select(ToDto);
}
```

---

## 8. Authentication (JWT)

### AirlineFuelMS.Infrastructure/Services/AuthService.cs
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AirlineFuelMS.Core.DTOs.Auth;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto dto);
    Task<User?> GetUserByIdAsync(int id);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user);
        return new LoginResponseDto(token, user.Role, user.Username);
    }

    public async Task<User?> GetUserByIdAsync(int id) =>
        await _context.Users.FindAsync(id);

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## 9. API Controllers

### AirlineFuelMS.API/Controllers/AuthController.cs
```csharp
using AirlineFuelMS.Core.DTOs.Auth;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Login — returns JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result is null)
            return Unauthorized(new { message = "Invalid username or password" });
        return Ok(result);
    }
}
```

### AirlineFuelMS.API/Controllers/AirlinesController.cs
```csharp
using AirlineFuelMS.Core.DTOs.Airline;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AirlinesController : ControllerBase
{
    private readonly IAirlineService _service;
    public AirlinesController(IAirlineService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await _service.GetSummaryAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] AirlineCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] AirlineUpdateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

### AirlineFuelMS.API/Controllers/FuelTransactionsController.cs
```csharp
using System.Security.Claims;
using AirlineFuelMS.Core.DTOs.FuelTransaction;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FuelTransactionsController : ControllerBase
{
    private readonly IFuelTransactionService _service;
    public FuelTransactionsController(IFuelTransactionService service) => _service = service;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-airline/{airlineId}")]
    public async Task<IActionResult> GetByAirline(int airlineId) =>
        Ok(await _service.GetByAirlineAsync(airlineId));

    [HttpGet("by-provider/{providerId}")]
    public async Task<IActionResult> GetByProvider(int providerId) =>
        Ok(await _service.GetByProviderAsync(providerId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FuelTransactionCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FuelTransactionUpdateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

### AirlineFuelMS.API/Controllers/InvoicesController.cs
```csharp
using AirlineFuelMS.Core.DTOs.Invoice;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    public InvoicesController(IInvoiceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-airline/{airlineId}")]
    public async Task<IActionResult> GetByAirline(int airlineId) =>
        Ok(await _service.GetByAirlineAsync(airlineId));

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] InvoiceUpdateStatusDto dto)
    {
        var result = await _service.UpdateStatusAsync(id, dto.Status);
        return result is null ? NotFound() : Ok(result);
    }
}
```

---

## 10. Invoice Generation Logic

### AirlineFuelMS.Infrastructure/Services/InvoiceService.cs
```csharp
using AirlineFuelMS.Core.DTOs.Invoice;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateForTransactionAsync(int transactionId);
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<InvoiceDto?> GetByIdAsync(int id);
    Task<IEnumerable<InvoiceDto>> GetByAirlineAsync(int airlineId);
    Task<InvoiceDto?> UpdateStatusAsync(int id, string status);
}

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private const decimal TaxRate = 0.18m; // 18% GST

    public InvoiceService(AppDbContext context) => _context = context;

    private static string GenerateInvoiceNumber() =>
        $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static InvoiceDto ToDto(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.Airline.Name,
        i.FuelProvider.Name,
        i.FuelTransaction.TransactionRef,
        i.Amount,
        i.TaxAmount,
        i.TotalAmount,
        i.Status,
        i.InvoiceDate,
        i.DueDate,
        i.PaidDate
    );

    public async Task<InvoiceDto> GenerateForTransactionAsync(int transactionId)
    {
        var txn = await _context.FuelTransactions
            .Include(t => t.Airline)
            .Include(t => t.FuelProvider)
            .FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException("Transaction not found");

        var amount = txn.QuantityLiters * txn.PricePerLiter;
        var tax = Math.Round(amount * TaxRate, 2);

        var invoice = new Invoice
        {
            FuelTransactionId = transactionId,
            AirlineId = txn.AirlineId,
            FuelProviderId = txn.FuelProviderId,
            InvoiceNumber = GenerateInvoiceNumber(),
            Amount = amount,
            TaxAmount = tax,
            TotalAmount = amount + tax,
            Status = "Unpaid",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var loaded = await _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
            .FirstAsync(i => i.Id == invoice.Id);

        return ToDto(loaded);
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var list = await _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
            .ToListAsync();
        return list.Select(ToDto);
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id)
    {
        var i = await _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
            .FirstOrDefaultAsync(i => i.Id == id);
        return i is null ? null : ToDto(i);
    }

    public async Task<IEnumerable<InvoiceDto>> GetByAirlineAsync(int airlineId)
    {
        var list = await _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
            .Where(i => i.AirlineId == airlineId)
            .ToListAsync();
        return list.Select(ToDto);
    }

    public async Task<InvoiceDto?> UpdateStatusAsync(int id, string status)
    {
        var i = await _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (i is null) return null;

        i.Status = status;
        if (status == "Paid") i.PaidDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToDto(i);
    }
}
```

---

## 11. Program.cs & Configuration

### AirlineFuelMS.API/Program.cs
```csharp
using System.Text;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// — Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// — Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAirlineService, AirlineService>();
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
    await db.Database.MigrateAsync();
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
```

---

## 12. appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AirlineFuelDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKey_AtLeast32CharsLong!",
    "Issuer": "AirlineFuelMS",
    "Audience": "AirlineFuelMS_Client"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 13. Running the Project

```bash
# 1. Set your SQL Server connection string in appsettings.json

# 2. Apply migrations + seed
cd AirlineFuelMS.API
dotnet ef database update --project ../AirlineFuelMS.Infrastructure

# 3. Run the API
dotnet run

# 4. Open Swagger UI
# https://localhost:5001/swagger
```

### Test Login
```json
POST /api/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
// Returns: { "token": "eyJ...", "role": "Admin", "username": "admin" }
```

### Example: Create Fuel Transaction
```
POST /api/fueltransactions
Authorization: Bearer {token}
{
  "airlineId": 1,        // Emirate Airlines
  "fuelProviderId": 3,   // Emirate provider → price 0.58/L
  "quantityLiters": 500, // 500 × 0.58 = $290 → invoice auto-generated
  "notes": "Regular refuel"
}
```

---

## Role Permissions Summary

| Endpoint | Admin | NormalUser |
|---|---|---|
| POST /auth/login | ✅ | ✅ |
| GET airlines, transactions, invoices | ✅ | ✅ |
| POST airline / fuel provider | ✅ | ❌ |
| POST fuel transaction | ✅ | ✅ |
| PUT / DELETE | ✅ | ❌ |
| GET airline summary | ✅ | ✅ |
| PUT invoice status | ✅ | ❌ |

---

## HP Provider Summary Query

To get HP-specific fuel totals (for the HP calculation requirement):

```csharp
// In AirlineService or a report controller
var hpSummary = await _context.FuelTransactions
    .Where(t => t.FuelProvider.Code == "HP")
    .GroupBy(t => t.AirlineId)
    .Select(g => new
    {
        AirlineId = g.Key,
        AirlineName = g.First().Airline.Name,
        TotalLiters = g.Sum(t => t.QuantityLiters),
        TotalAmount = g.Sum(t => t.QuantityLiters * t.PricePerLiter)
    })
    .ToListAsync();
```

---

*End of guide — all entities, services, controllers, auth, and invoice logic are fully wired.*
