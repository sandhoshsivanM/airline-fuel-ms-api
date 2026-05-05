using AirlineFuelMS.Core.DTOs.Common;

namespace AirlineFuelMS.Core.DTOs.Invoice;

public record InvoiceDto(
    int Id,
    string InvoiceNumber,
    string AirlineName,
    string FuelProviderName,
    string TransactionRef,
    int FuelTransactionId,
    string CountryName,
    string CurrencyCode,
    string CurrencySymbol,
    decimal Amount,
    decimal TaxAmount,
    decimal TotalAmount,
    /// <summary>
    /// Effective status — auto-flips Unpaid → Overdue when DueDate is in the past.
    /// (The stored Status only changes if an admin explicitly sets it.)
    /// </summary>
    string Status,
    string StoredStatus,
    int DaysOverdue,
    DateTime InvoiceDate,
    DateTime DueDate,
    DateTime? PaidDate
);

public record InvoiceUpdateStatusDto(string Status);

/// <summary>Explicit invoice creation: pick an existing transaction.</summary>
public record InvoiceCreateDto(
    int FuelTransactionId,
    /// <summary>Optional override; defaults to txn date + 30 days.</summary>
    DateTime? DueDate
);

public class InvoiceQuery : PagedQuery
{
    public int? AirlineId { get; init; }
    public int? FuelProviderId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
