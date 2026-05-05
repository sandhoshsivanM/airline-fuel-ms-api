using AirlineFuelMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Users
        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), Email = "admin@airlinefuel.com", Role = "Admin" },
                new User { Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),  Email = "user1@airlinefuel.com", Role = "NormalUser" }
            );
        }

        // 2. Country master — now with currency
        if (!await context.Countries.AnyAsync())
        {
            context.Countries.AddRange(
                new Country { Name = "India",                Code = "IN", CurrencyCode = "INR", CurrencySymbol = "₹"   },
                new Country { Name = "United Arab Emirates", Code = "AE", CurrencyCode = "AED", CurrencySymbol = "د.إ" },
                new Country { Name = "Singapore",            Code = "SG", CurrencyCode = "SGD", CurrencySymbol = "S$"  }
            );
        }
        await context.SaveChangesAsync();

        var indiaId = (await context.Countries.FirstAsync(c => c.Code == "IN")).Id;
        var uaeId   = (await context.Countries.FirstAsync(c => c.Code == "AE")).Id;

        // 3. Fuel Providers (companies — country lives on addresses)
        if (!await context.FuelProviders.AnyAsync())
        {
            context.FuelProviders.AddRange(
                new FuelProvider { Name = "HP (Hindustan Petroleum)", Code = "HP",  ContactInfo = "hp@petroleum.com" },
                new FuelProvider { Name = "Bharat Petroleum",         Code = "BP",  ContactInfo = "contact@bharatpetroleum.com" },
                new FuelProvider { Name = "Emirate Fuel Services",    Code = "EMI", ContactInfo = "fuel@emirate.com" }
            );
        }
        await context.SaveChangesAsync();

        var hp  = await context.FuelProviders.FirstAsync(p => p.Code == "HP");
        var bp  = await context.FuelProviders.FirstAsync(p => p.Code == "BP");
        var emi = await context.FuelProviders.FirstAsync(p => p.Code == "EMI");

        // 4. Provider addresses — HP in BOTH India + UAE to demo multi-country
        if (!await context.FuelProviderAddresses.AnyAsync())
        {
            context.FuelProviderAddresses.AddRange(
                new FuelProviderAddress { FuelProviderId = hp.Id,  CountryId = indiaId, AddressLine1 = "Hindustan House, Churchgate", City = "Mumbai", PostalCode = "400020", IsHeadOffice = true  },
                new FuelProviderAddress { FuelProviderId = hp.Id,  CountryId = uaeId,   AddressLine1 = "Sheikh Zayed Rd Branch",      City = "Dubai",  PostalCode = "00000",  IsHeadOffice = false },
                new FuelProviderAddress { FuelProviderId = bp.Id,  CountryId = indiaId, AddressLine1 = "Bharat Bhavan, Ballard Estate", City = "Mumbai", PostalCode = "400001", IsHeadOffice = true },
                new FuelProviderAddress { FuelProviderId = emi.Id, CountryId = uaeId,   AddressLine1 = "Emirate Tower, DIFC",         City = "Dubai",  PostalCode = "00000",  IsHeadOffice = true  }
            );
        }

        // 5. Fuel Prices
        if (!await context.FuelPrices.AnyAsync())
        {
            context.FuelPrices.AddRange(
                new FuelPrice { FuelProviderId = hp.Id,  PricePerLiter = 0.72m, EffectiveFrom = DateTime.UtcNow, IsActive = true },
                new FuelPrice { FuelProviderId = bp.Id,  PricePerLiter = 0.68m, EffectiveFrom = DateTime.UtcNow, IsActive = true },
                new FuelPrice { FuelProviderId = emi.Id, PricePerLiter = 0.58m, EffectiveFrom = DateTime.UtcNow, IsActive = true }
            );
        }

        // 6. Airlines (aircraft master)
        if (!await context.Airlines.AnyAsync())
        {
            context.Airlines.AddRange(
                new Airline { Name = "Emirate Airlines EK521", Code = "EK", Model = "Airbus A380-800", PassengerCapacity = 615, FuelTankCapacityLiters = 1000, Country = "UAE"   },
                new Airline { Name = "IndiGo 6E1414",          Code = "6E", Model = "Airbus A320neo",  PassengerCapacity = 186, FuelTankCapacityLiters = 5000, Country = "India" },
                new Airline { Name = "Air India AI144",        Code = "AI", Model = "Boeing 787-8",    PassengerCapacity = 256, FuelTankCapacityLiters = 8000, Country = "India" }
            );
        }
        await context.SaveChangesAsync();
    }
}
