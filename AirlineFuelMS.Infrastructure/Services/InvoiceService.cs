using AirlineFuelMS.Core.DTOs.Common;
using AirlineFuelMS.Core.DTOs.Invoice;
using AirlineFuelMS.Core.Entities;
using AirlineFuelMS.Infrastructure.Data;
using AirlineFuelMS.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AirlineFuelMS.Infrastructure.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateForTransactionAsync(int transactionId, DateTime? dueDate);
    Task<InvoiceDto> CreateExplicitAsync(InvoiceCreateDto dto);
    Task<PagedResult<InvoiceDto>> GetAllAsync(InvoiceQuery query);
    Task<InvoiceDto?> GetByIdAsync(int id);
    Task<InvoiceDto?> UpdateStatusAsync(int id, string status);
}

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private const decimal TaxRate = 0.18m; // 18% GST

    public InvoiceService(AppDbContext context) => _context = context;

    private static string GenerateInvoiceNumber() =>
        $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    /// <summary>Compute effective status with auto-overdue, plus daysOverdue.</summary>
    private static (string EffectiveStatus, int DaysOverdue) Effective(string stored, DateTime dueDate, DateTime? paidDate)
    {
        // If paid, no overdue concept.
        if (string.Equals(stored, "Paid", StringComparison.OrdinalIgnoreCase))
            return ("Paid", 0);

        var today = DateTime.UtcNow.Date;
        var due = dueDate.Date;
        if (due < today)
        {
            var days = (today - due).Days;
            // If admin set "Overdue" already, keep; otherwise auto-flip from Unpaid.
            return ("Overdue", days);
        }
        return (stored, 0);
    }

    private static InvoiceDto ToDto(Invoice i)
    {
        var (effective, days) = Effective(i.Status, i.DueDate, i.PaidDate);
        var country = i.FuelTransaction?.FuelProviderAddress?.Country;
        return new InvoiceDto(
            i.Id,
            i.InvoiceNumber,
            i.Airline?.Name ?? string.Empty,
            i.FuelProvider?.Name ?? string.Empty,
            i.FuelTransaction?.TransactionRef ?? string.Empty,
            i.FuelTransactionId,
            country?.Name ?? string.Empty,
            country?.CurrencyCode ?? string.Empty,
            country?.CurrencySymbol ?? string.Empty,
            i.Amount,
            i.TaxAmount,
            i.TotalAmount,
            effective,
            i.Status,
            days,
            i.InvoiceDate,
            i.DueDate,
            i.PaidDate
        );
    }

    private IQueryable<Invoice> BaseQuery() =>
        _context.Invoices
            .Include(i => i.Airline)
            .Include(i => i.FuelProvider)
            .Include(i => i.FuelTransaction)
                .ThenInclude(t => t.FuelProviderAddress)
                .ThenInclude(a => a.Country);

    public async Task<InvoiceDto> GenerateForTransactionAsync(int transactionId, DateTime? dueDate)
    {
        var txn = await _context.FuelTransactions
            .Include(t => t.Airline)
            .Include(t => t.FuelProvider)
            .Include(t => t.Invoice)
            .Include(t => t.FuelProviderAddress).ThenInclude(a => a.Country)
            .FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException("Transaction not found");

        if (txn.Invoice is not null)
            throw new InvalidOperationException(
                $"Transaction {txn.TransactionRef} already has invoice {txn.Invoice.InvoiceNumber}.");

        var amount = txn.QuantityLiters * txn.PricePerLiter;
        var tax = Math.Round(amount * TaxRate, 2);
        var due = dueDate ?? DateTime.UtcNow.AddDays(30);

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
            DueDate = due
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var loaded = await BaseQuery().FirstAsync(i => i.Id == invoice.Id);
        return ToDto(loaded);
    }

    public Task<InvoiceDto> CreateExplicitAsync(InvoiceCreateDto dto) =>
        GenerateForTransactionAsync(dto.FuelTransactionId, dto.DueDate);

    public async Task<PagedResult<InvoiceDto>> GetAllAsync(InvoiceQuery query)
    {
        var q = BaseQuery();

        var filterKeys = new Dictionary<string, int>();
        if (query.AirlineId.HasValue)      filterKeys["AirlineId"]      = query.AirlineId.Value;
        if (query.FuelProviderId.HasValue) filterKeys["FuelProviderId"] = query.FuelProviderId.Value;
        q = q.ApplyFilter(query.Search, filterKeys);

        if (query.FromDate.HasValue) q = q.Where(i => i.InvoiceDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)   q = q.Where(i => i.InvoiceDate <= query.ToDate.Value);

        // Status filter is a bit special: caller may ask for "Overdue" which is a
        // computed state. Translate it to "stored=Unpaid AND dueDate < today".
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var today = DateTime.UtcNow.Date;
            q = query.Status switch
            {
                "Overdue" => q.Where(i => i.Status != "Paid" && i.DueDate < today),
                "Unpaid"  => q.Where(i => i.Status == "Unpaid" && i.DueDate >= today),
                _         => q.Where(i => i.Status == query.Status),
            };
        }

        q = (query.SortBy?.ToLowerInvariant(), query.IsDescending) switch
        {
            ("invoicenumber", true)  => q.OrderByDescending(i => i.InvoiceNumber),
            ("invoicenumber", false) => q.OrderBy(i => i.InvoiceNumber),
            ("amount",        true)  => q.OrderByDescending(i => i.Amount),
            ("amount",        false) => q.OrderBy(i => i.Amount),
            ("totalamount",   true)  => q.OrderByDescending(i => i.TotalAmount),
            ("totalamount",   false) => q.OrderBy(i => i.TotalAmount),
            ("status",        true)  => q.OrderByDescending(i => i.Status),
            ("status",        false) => q.OrderBy(i => i.Status),
            ("invoicedate",   true)  => q.OrderByDescending(i => i.InvoiceDate),
            ("invoicedate",   false) => q.OrderBy(i => i.InvoiceDate),
            ("duedate",       true)  => q.OrderByDescending(i => i.DueDate),
            ("duedate",       false) => q.OrderBy(i => i.DueDate),
            (_,               true)  => q.OrderByDescending(i => i.Id),
            _                        => q.OrderByDescending(i => i.Id),
        };

        var total = await q.CountAsync();
        var entities = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        return PagedResult<InvoiceDto>.Create(entities.Select(ToDto), total, query.Page, query.PageSize);
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id)
    {
        var i = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        return i is null ? null : ToDto(i);
    }

    public async Task<InvoiceDto?> UpdateStatusAsync(int id, string status)
    {
        var i = await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        if (i is null) return null;
        i.Status = status;
        if (status == "Paid") i.PaidDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ToDto(i);
    }
}
