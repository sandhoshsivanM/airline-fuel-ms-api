using AirlineFuelMS.Core.DTOs.Common;
using AirlineFuelMS.Core.DTOs.FuelProvider;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IFuelProviderService
{
    Task<PagedResult<FuelProviderDto>> GetAllAsync(FuelProviderQuery query);
    Task<FuelProviderDto?> GetByIdAsync(int id);
    Task<FuelProviderDto> CreateAsync(FuelProviderCreateDto dto);
    Task<FuelProviderDto?> UpdateAsync(int id, FuelProviderUpdateDto dto);
    Task<bool> DeleteAsync(int id);

    Task<IEnumerable<FuelPriceDto>> GetPricesAsync(int providerId);
    Task<FuelPriceDto?> AddPriceAsync(int providerId, FuelPriceCreateDto dto);

    Task<IEnumerable<FuelProviderAddressDto>> GetAddressesAsync(int providerId);
    Task<FuelProviderAddressDto?> AddAddressAsync(int providerId, FuelProviderAddressCreateDto dto);
    Task<FuelProviderAddressDto?> UpdateAddressAsync(int providerId, int addressId, FuelProviderAddressUpdateDto dto);
    Task<bool> DeleteAddressAsync(int providerId, int addressId);

    Task<IEnumerable<CountryDto>> GetCountriesAsync();
}

public class FuelProviderService : IFuelProviderService
{
    private readonly AppDbContext _context;
    public FuelProviderService(AppDbContext context) => _context = context;

    private static FuelProviderAddressDto ToDto(FuelProviderAddress a) => new(
        a.Id, a.FuelProviderId,
        a.CountryId, a.Country?.Name ?? string.Empty,
        a.Country?.CurrencyCode ?? string.Empty,
        a.Country?.CurrencySymbol ?? string.Empty,
        a.AddressLine1, a.City, a.PostalCode, a.IsHeadOffice, a.IsActive, a.CreatedAt
    );

    private static FuelProviderDto ToDto(FuelProvider p, decimal? currentPrice)
    {
        var addresses = p.Addresses?.Select(ToDto).ToList() ?? new List<FuelProviderAddressDto>();
        var countryIds = addresses.Where(a => a.IsActive).Select(a => a.CountryId).Distinct().ToList();
        return new FuelProviderDto(
            p.Id, p.Name, p.Code, p.ContactInfo, p.IsActive, currentPrice, p.CreatedAt,
            addresses, countryIds
        );
    }

    private static FuelPriceDto ToDto(FuelPrice fp) => new(
        fp.Id, fp.FuelProviderId, fp.PricePerLiter, fp.EffectiveFrom, fp.EffectiveTo, fp.IsActive
    );

    public async Task<PagedResult<FuelProviderDto>> GetAllAsync(FuelProviderQuery query)
    {
        var q = _context.FuelProviders
            .Include(p => p.Addresses).ThenInclude(a => a.Country)
            .AsQueryable();

        var filterKeys = new Dictionary<string, int>();
        if (query.IsActive.HasValue) filterKeys["IsActive"] = query.IsActive.Value ? 1 : 0;
        q = q.ApplyFilter(query.Search, filterKeys);

        if (query.CountryId.HasValue)
            q = q.Where(p => p.Addresses.Any(a => a.CountryId == query.CountryId.Value && a.IsActive));

        q = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("name",      true)  => q.OrderByDescending(p => p.Name),
            ("name",      false) => q.OrderBy(p => p.Name),
            ("code",      true)  => q.OrderByDescending(p => p.Code),
            ("code",      false) => q.OrderBy(p => p.Code),
            ("isactive",  true)  => q.OrderByDescending(p => p.IsActive),
            ("isactive",  false) => q.OrderBy(p => p.IsActive),
            ("createdat", true)  => q.OrderByDescending(p => p.CreatedAt),
            ("createdat", false) => q.OrderBy(p => p.CreatedAt),
            (_,           true)  => q.OrderByDescending(p => p.Id),
            _                    => q.OrderBy(p => p.Id),
        };

        var total = await q.CountAsync();
        var page = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        var ids = page.Select(p => p.Id).ToList();
        var prices = await _context.FuelPrices
            .Where(fp => ids.Contains(fp.FuelProviderId) && fp.IsActive)
            .GroupBy(fp => fp.FuelProviderId)
            .Select(g => new { ProviderId = g.Key, Price = g.OrderByDescending(x => x.EffectiveFrom).First().PricePerLiter })
            .ToDictionaryAsync(x => x.ProviderId, x => (decimal?)x.Price);

        var items = page.Select(p => ToDto(p, prices.GetValueOrDefault(p.Id)));
        return PagedResult<FuelProviderDto>.Create(items, total, query.Page, query.PageSize);
    }

    public async Task<FuelProviderDto?> GetByIdAsync(int id)
    {
        var p = await _context.FuelProviders
            .Include(x => x.Addresses).ThenInclude(a => a.Country)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;

        var current = await _context.FuelPrices
            .Where(fp => fp.FuelProviderId == id && fp.IsActive)
            .OrderByDescending(fp => fp.EffectiveFrom)
            .Select(fp => (decimal?)fp.PricePerLiter)
            .FirstOrDefaultAsync();

        return ToDto(p, current);
    }

    public async Task<FuelProviderDto> CreateAsync(FuelProviderCreateDto dto)
    {
        var code = dto.Code.ToUpper();
        if (await _context.FuelProviders.AnyAsync(p => p.Code == code))
            throw new InvalidOperationException($"Fuel provider with code '{code}' already exists.");

        if (dto.InitialAddress is { } addr &&
            !await _context.Countries.AnyAsync(c => c.Id == addr.CountryId))
            throw new KeyNotFoundException($"Country {addr.CountryId} not found.");

        var provider = new FuelProvider { Name = dto.Name, Code = code, ContactInfo = dto.ContactInfo };
        _context.FuelProviders.Add(provider);
        await _context.SaveChangesAsync();

        if (dto.InitialAddress is { } a)
        {
            _context.FuelProviderAddresses.Add(new FuelProviderAddress
            {
                FuelProviderId = provider.Id, CountryId = a.CountryId,
                AddressLine1 = a.AddressLine1, City = a.City, PostalCode = a.PostalCode,
                IsHeadOffice = a.IsHeadOffice, IsActive = true,
            });
        }

        decimal? initialPrice = null;
        if (dto.InitialPricePerLiter is { } price && price > 0)
        {
            _context.FuelPrices.Add(new FuelPrice
            {
                FuelProviderId = provider.Id, PricePerLiter = price,
                EffectiveFrom = DateTime.UtcNow, IsActive = true
            });
            initialPrice = price;
        }
        await _context.SaveChangesAsync();

        var loaded = await _context.FuelProviders
            .Include(x => x.Addresses).ThenInclude(ad => ad.Country)
            .FirstAsync(x => x.Id == provider.Id);
        return ToDto(loaded, initialPrice);
    }

    public async Task<FuelProviderDto?> UpdateAsync(int id, FuelProviderUpdateDto dto)
    {
        var p = await _context.FuelProviders
            .Include(x => x.Addresses).ThenInclude(a => a.Country)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;

        p.Name = dto.Name; p.ContactInfo = dto.ContactInfo; p.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        var current = await _context.FuelPrices
            .Where(fp => fp.FuelProviderId == id && fp.IsActive)
            .OrderByDescending(fp => fp.EffectiveFrom)
            .Select(fp => (decimal?)fp.PricePerLiter)
            .FirstOrDefaultAsync();

        return ToDto(p, current);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _context.FuelProviders.FindAsync(id);
        if (p is null) return false;
        p.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FuelPriceDto>> GetPricesAsync(int providerId) =>
        await _context.FuelPrices
            .Where(fp => fp.FuelProviderId == providerId)
            .OrderByDescending(fp => fp.EffectiveFrom)
            .Select(fp => new FuelPriceDto(fp.Id, fp.FuelProviderId, fp.PricePerLiter, fp.EffectiveFrom, fp.EffectiveTo, fp.IsActive))
            .ToListAsync();

    public async Task<FuelPriceDto?> AddPriceAsync(int providerId, FuelPriceCreateDto dto)
    {
        if (!await _context.FuelProviders.AnyAsync(p => p.Id == providerId)) return null;
        var effectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow;
        var current = await _context.FuelPrices.Where(fp => fp.FuelProviderId == providerId && fp.IsActive).ToListAsync();
        foreach (var c in current) { c.IsActive = false; c.EffectiveTo = effectiveFrom; }
        var newPrice = new FuelPrice
        {
            FuelProviderId = providerId, PricePerLiter = dto.PricePerLiter,
            EffectiveFrom = effectiveFrom, IsActive = true
        };
        _context.FuelPrices.Add(newPrice);
        await _context.SaveChangesAsync();
        return ToDto(newPrice);
    }

    public async Task<IEnumerable<FuelProviderAddressDto>> GetAddressesAsync(int providerId) =>
        await _context.FuelProviderAddresses
            .Include(a => a.Country)
            .Where(a => a.FuelProviderId == providerId)
            .OrderByDescending(a => a.IsHeadOffice).ThenBy(a => a.City)
            .Select(a => new FuelProviderAddressDto(
                a.Id, a.FuelProviderId, a.CountryId, a.Country.Name,
                a.Country.CurrencyCode, a.Country.CurrencySymbol,
                a.AddressLine1, a.City, a.PostalCode, a.IsHeadOffice, a.IsActive, a.CreatedAt))
            .ToListAsync();

    public async Task<FuelProviderAddressDto?> AddAddressAsync(int providerId, FuelProviderAddressCreateDto dto)
    {
        if (!await _context.FuelProviders.AnyAsync(p => p.Id == providerId)) return null;
        if (!await _context.Countries.AnyAsync(c => c.Id == dto.CountryId))
            throw new KeyNotFoundException($"Country {dto.CountryId} not found.");

        if (dto.IsHeadOffice)
        {
            var others = await _context.FuelProviderAddresses
                .Where(a => a.FuelProviderId == providerId && a.IsHeadOffice).ToListAsync();
            foreach (var o in others) o.IsHeadOffice = false;
        }

        var entity = new FuelProviderAddress
        {
            FuelProviderId = providerId, CountryId = dto.CountryId,
            AddressLine1 = dto.AddressLine1, City = dto.City, PostalCode = dto.PostalCode,
            IsHeadOffice = dto.IsHeadOffice, IsActive = true
        };
        _context.FuelProviderAddresses.Add(entity);
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(e => e.Country).LoadAsync();
        return ToDto(entity);
    }

    public async Task<FuelProviderAddressDto?> UpdateAddressAsync(int providerId, int addressId, FuelProviderAddressUpdateDto dto)
    {
        var entity = await _context.FuelProviderAddresses
            .Include(a => a.Country)
            .FirstOrDefaultAsync(a => a.Id == addressId && a.FuelProviderId == providerId);
        if (entity is null) return null;

        if (entity.CountryId != dto.CountryId &&
            !await _context.Countries.AnyAsync(c => c.Id == dto.CountryId))
            throw new KeyNotFoundException($"Country {dto.CountryId} not found.");

        if (dto.IsHeadOffice && !entity.IsHeadOffice)
        {
            var others = await _context.FuelProviderAddresses
                .Where(a => a.FuelProviderId == providerId && a.IsHeadOffice && a.Id != addressId).ToListAsync();
            foreach (var o in others) o.IsHeadOffice = false;
        }

        entity.CountryId = dto.CountryId;
        entity.AddressLine1 = dto.AddressLine1;
        entity.City = dto.City;
        entity.PostalCode = dto.PostalCode;
        entity.IsHeadOffice = dto.IsHeadOffice;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(a => a.Country).LoadAsync();
        return ToDto(entity);
    }

    public async Task<bool> DeleteAddressAsync(int providerId, int addressId)
    {
        var entity = await _context.FuelProviderAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.FuelProviderId == providerId);
        if (entity is null) return false;
        entity.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CountryDto>> GetCountriesAsync() =>
        await _context.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Name, c.Code, c.CurrencyCode, c.CurrencySymbol, c.IsActive))
            .ToListAsync();
}
