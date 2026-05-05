using AirlineFuelMS.Core.DTOs.Airline;
using AirlineFuelMS.Core.DTOs.Common;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IAirlineService
{
    Task<PagedResult<AirlineDto>> GetAllAsync(AirlineQuery query);
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

    private static AirlineDto ToDto(Airline a) => new(
        a.Id, a.Name, a.Code, a.Model, a.PassengerCapacity,
        a.FuelTankCapacityLiters, a.Country, a.IsActive
    );

    public async Task<PagedResult<AirlineDto>> GetAllAsync(AirlineQuery query)
    {
        var q = _context.Airlines.AsQueryable();

        // Generic ApplyFilter (search + int dictionary)
        var filterKeys = new Dictionary<string, int>();
        if (query.IsActive.HasValue) filterKeys["IsActive"] = query.IsActive.Value ? 1 : 0;
        q = q.ApplyFilter(query.Search, filterKeys);

        if (!string.IsNullOrWhiteSpace(query.Country))
            q = q.Where(a => a.Country == query.Country);

        // Sort
        q = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("name",                  true)  => q.OrderByDescending(a => a.Name),
            ("name",                  false) => q.OrderBy(a => a.Name),
            ("code",                  true)  => q.OrderByDescending(a => a.Code),
            ("code",                  false) => q.OrderBy(a => a.Code),
            ("model",                 true)  => q.OrderByDescending(a => a.Model),
            ("model",                 false) => q.OrderBy(a => a.Model),
            ("country",               true)  => q.OrderByDescending(a => a.Country),
            ("country",               false) => q.OrderBy(a => a.Country),
            ("fueltankcapacityliters", true)  => q.OrderByDescending(a => a.FuelTankCapacityLiters),
            ("fueltankcapacityliters", false) => q.OrderBy(a => a.FuelTankCapacityLiters),
            ("passengercapacity",     true)  => q.OrderByDescending(a => a.PassengerCapacity),
            ("passengercapacity",     false) => q.OrderBy(a => a.PassengerCapacity),
            ("isactive",              true)  => q.OrderByDescending(a => a.IsActive),
            ("isactive",              false) => q.OrderBy(a => a.IsActive),
            (_,                       true)  => q.OrderByDescending(a => a.Id),
            _                                => q.OrderBy(a => a.Id),
        };

        var total = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AirlineDto(a.Id, a.Name, a.Code, a.Model, a.PassengerCapacity,
                                        a.FuelTankCapacityLiters, a.Country, a.IsActive))
            .ToListAsync();

        return PagedResult<AirlineDto>.Create(items, total, query.Page, query.PageSize);
    }

    public async Task<AirlineDto?> GetByIdAsync(int id)
    {
        var a = await _context.Airlines.FindAsync(id);
        return a is null ? null : ToDto(a);
    }

    public async Task<AirlineDto> CreateAsync(AirlineCreateDto dto)
    {
        var airline = new Airline
        {
            Name = dto.Name,
            Code = dto.Code.ToUpper(),
            Model = dto.Model,
            PassengerCapacity = dto.PassengerCapacity,
            FuelTankCapacityLiters = dto.FuelTankCapacityLiters,
            Country = dto.Country
        };
        _context.Airlines.Add(airline);
        await _context.SaveChangesAsync();
        return ToDto(airline);
    }

    public async Task<AirlineDto?> UpdateAsync(int id, AirlineUpdateDto dto)
    {
        var airline = await _context.Airlines.FindAsync(id);
        if (airline is null) return null;
        airline.Name = dto.Name;
        airline.Model = dto.Model;
        airline.PassengerCapacity = dto.PassengerCapacity;
        airline.FuelTankCapacityLiters = dto.FuelTankCapacityLiters;
        airline.Country = dto.Country;
        airline.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();
        return ToDto(airline);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var airline = await _context.Airlines.FindAsync(id);
        if (airline is null) return false;
        airline.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AirlineSummaryDto>> GetSummaryAsync()
    {
        return await _context.Airlines
            .Where(a => a.IsActive)
            .Select(a => new AirlineSummaryDto(
                a.Id, a.Name, a.Code,
                a.FuelTransactions.Sum(t => t.QuantityLiters),
                a.FuelTransactions.Sum(t => t.QuantityLiters * t.PricePerLiter),
                a.FuelTransactions.Count(),
                a.Invoices.Count(i => i.Status == "Unpaid" || i.Status == "Overdue"),
                a.Invoices.Count(i => i.Status == "Paid")
            ))
            .ToListAsync();
    }
}
