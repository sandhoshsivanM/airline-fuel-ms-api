using AirlineFuelMS.Core.DTOs.Common;

namespace AirlineFuelMS.Core.DTOs.FuelTransaction;

public record FuelTransactionCreateDto(
    int AirlineId,
    int FuelProviderId,
    /// <summary>The specific provider address (location) where the fueling happens.
    /// Determines country &amp; currency. Must belong to the chosen provider.</summary>
    int FuelProviderAddressId,
    decimal QuantityLiters,
    string? Notes,
    /// <summary>If true (default), an invoice is auto-generated immediately.
    /// If false, the invoice can be created later from the Invoices screen.</summary>
    bool GenerateInvoice = true
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
    int FuelProviderAddressId,
    string LocationCity,
    string CountryName,
    string CurrencyCode,
    string CurrencySymbol,
    decimal QuantityLiters,
    decimal PricePerLiter,
    decimal TotalAmount,
    string TransactionRef,
    string Status,
    DateTime TransactionDate,
    string? Notes,
    bool HasInvoice
);

public class FuelTransactionQuery : PagedQuery
{
    public int? AirlineId { get; init; }
    public int? FuelProviderId { get; init; }
    public int? FuelProviderAddressId { get; init; }
    public int? CountryId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    /// <summary>true = only transactions WITHOUT an invoice; false = only WITH; null = all</summary>
    public bool? HasInvoice { get; init; }
}
