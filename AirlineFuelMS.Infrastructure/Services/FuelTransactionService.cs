using AirlineFuelMS.Core.DTOs.Common;
using AirlineFuelMS.Core.DTOs.FuelTransaction;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IFuelTransactionService
{
    Task<PagedResult<FuelTransactionDto>> GetAllAsync(FuelTransactionQuery query);
    Task<FuelTransactionDto?> GetByIdAsync(int id);
    Task<FuelTransactionDto> CreateAsync(FuelTransactionCreateDto dto, int userId);
    Task<FuelTransactionDto?> UpdateAsync(int id, FuelTransactionUpdateDto dto);
    Task<bool> DeleteAsync(int id);
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
        t.FuelProviderAddressId,
        t.FuelProviderAddress?.City ?? string.Empty,
        t.FuelProviderAddress?.Country?.Name ?? string.Empty,
        t.FuelProviderAddress?.Country?.CurrencyCode ?? string.Empty,
        t.FuelProviderAddress?.Country?.CurrencySymbol ?? string.Empty,
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
            .Include(t => t.FuelProviderAddress).ThenInclude(a => a.Country)
            .Include(t => t.Invoice);

    public async Task<PagedResult<FuelTransactionDto>> GetAllAsync(FuelTransactionQuery query)
    {
        var q = BaseQuery();

        var filterKeys = new Dictionary<string, int>();
        if (query.AirlineId.HasValue)             filterKeys["AirlineId"]             = query.AirlineId.Value;
        if (query.FuelProviderId.HasValue)        filterKeys["FuelProviderId"]        = query.FuelProviderId.Value;
        if (query.FuelProviderAddressId.HasValue) filterKeys["FuelProviderAddressId"] = query.FuelProviderAddressId.Value;
        q = q.ApplyFilter(query.Search, filterKeys);

        if (query.CountryId.HasValue)
            q = q.Where(t => t.FuelProviderAddress.CountryId == query.CountryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(t => t.Status == query.Status);
        if (query.FromDate.HasValue)
            q = q.Where(t => t.TransactionDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(t => t.TransactionDate <= query.ToDate.Value);
        if (query.HasInvoice.HasValue)
        {
            q = query.HasInvoice.Value
                ? q.Where(t => t.Invoice != null)
                : q.Where(t => t.Invoice == null);
        }

        q = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("transactiondate",  true)  => q.OrderByDescending(t => t.TransactionDate),
            ("transactiondate",  false) => q.OrderBy(t => t.TransactionDate),
            ("quantityliters",   true)  => q.OrderByDescending(t => t.QuantityLiters),
            ("quantityliters",   false) => q.OrderBy(t => t.QuantityLiters),
            ("totalamount",      true)  => q.OrderByDescending(t => t.QuantityLiters * t.PricePerLiter),
            ("totalamount",      false) => q.OrderBy(t => t.QuantityLiters * t.PricePerLiter),
            ("status",           true)  => q.OrderByDescending(t => t.Status),
            ("status",           false) => q.OrderBy(t => t.Status),
            ("transactionref",   true)  => q.OrderByDescending(t => t.TransactionRef),
            ("transactionref",   false) => q.OrderBy(t => t.TransactionRef),
            (_,                  true)  => q.OrderByDescending(t => t.Id),
            _                           => q.OrderByDescending(t => t.Id),
        };

        var total = await q.CountAsync();
        var entities = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

        return PagedResult<FuelTransactionDto>.Create(entities.Select(ToDto), total, query.Page, query.PageSize);
    }

    public async Task<FuelTransactionDto?> GetByIdAsync(int id)
    {
        var t = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        return t is null ? null : ToDto(t);
    }

    public async Task<FuelTransactionDto> CreateAsync(FuelTransactionCreateDto dto, int userId)
    {
        // Resolve & validate the address — the source of country / currency.
        var address = await _context.FuelProviderAddresses
            .FirstOrDefaultAsync(a => a.Id == dto.FuelProviderAddressId)
            ?? throw new KeyNotFoundException("Fuel provider address not found.");

        if (address.FuelProviderId != dto.FuelProviderId)
            throw new InvalidOperationException(
                $"Address {dto.FuelProviderAddressId} does not belong to provider {dto.FuelProviderId}.");
        if (!address.IsActive)
            throw new InvalidOperationException("Selected address is inactive.");

        var price = await _context.FuelPrices
            .Where(p => p.FuelProviderId == dto.FuelProviderId && p.IsActive)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => p.PricePerLiter)
            .FirstOrDefaultAsync();
        if (price == 0)
            throw new InvalidOperationException("No active fuel price found for the selected provider.");

        var airline = await _context.Airlines.FindAsync(dto.AirlineId)
            ?? throw new KeyNotFoundException("Airline not found");

        if (dto.QuantityLiters > airline.FuelTankCapacityLiters)
            throw new InvalidOperationException(
                $"Requested {dto.QuantityLiters}L exceeds aircraft fuel tank capacity {airline.FuelTankCapacityLiters}L");

        var transaction = new FuelTransaction
        {
            AirlineId = dto.AirlineId,
            FuelProviderId = dto.FuelProviderId,
            FuelProviderAddressId = dto.FuelProviderAddressId,
            CreatedByUserId = userId,
            QuantityLiters = dto.QuantityLiters,
            PricePerLiter = price,
            TransactionRef = GenerateRef(),
            Status = "Completed",
            Notes = dto.Notes ?? string.Empty
        };

        _context.FuelTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        if (dto.GenerateInvoice)
            await _invoiceService.GenerateForTransactionAsync(transaction.Id, dueDate: null);

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
}
