using AirlineFuelMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<FuelProvider> FuelProviders => Set<FuelProvider>();
    public DbSet<FuelProviderAddress> FuelProviderAddresses => Set<FuelProviderAddress>();
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

        // Country (master)
        modelBuilder.Entity<Country>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.HasIndex(c => c.Name).IsUnique();
        });

        // FuelProvider — provider company; country lives on its addresses
        modelBuilder.Entity<FuelProvider>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
        });

        // FuelProviderAddress → FuelProvider, Country
        modelBuilder.Entity<FuelProviderAddress>(e =>
        {
            e.HasOne(a => a.FuelProvider)
             .WithMany(p => p.Addresses)
             .HasForeignKey(a => a.FuelProviderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Country)
             .WithMany(c => c.Addresses)
             .HasForeignKey(a => a.CountryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.FuelProviderId, a.CountryId });
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
            e.Ignore(t => t.TotalAmount); // computed in CLR (QuantityLiters * PricePerLiter); not stored
            e.Property(t => t.QuantityLiters).HasPrecision(12, 2);
            e.Property(t => t.PricePerLiter).HasPrecision(10, 4);

            e.HasOne(t => t.Airline)
             .WithMany(a => a.FuelTransactions)
             .HasForeignKey(t => t.AirlineId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.FuelProvider)
             .WithMany(p => p.FuelTransactions)
             .HasForeignKey(t => t.FuelProviderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.FuelProviderAddress)
             .WithMany()
             .HasForeignKey(t => t.FuelProviderAddressId)
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
