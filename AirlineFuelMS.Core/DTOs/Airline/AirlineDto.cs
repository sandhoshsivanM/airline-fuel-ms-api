using AirlineFuelMS.Core.DTOs.Common;

namespace AirlineFuelMS.Core.DTOs.Airline;

public record AirlineCreateDto(
    string Name,
    string Code,
    string Model,
    int PassengerCapacity,
    int FuelTankCapacityLiters,
    string Country
);

public record AirlineUpdateDto(
    string Name,
    string Model,
    int PassengerCapacity,
    int FuelTankCapacityLiters,
    string Country,
    bool IsActive
);

public record AirlineDto(
    int Id,
    string Name,
    string Code,
    string Model,
    int PassengerCapacity,
    int FuelTankCapacityLiters,
    string Country,
    bool IsActive
);

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

/// <summary>
/// Query for GET /api/airlines.
/// Sortable: id, name, code, model, country, fuelTankCapacityLiters, passengerCapacity, isActive.
/// Search matches Name OR Code OR Model (Search-attributed).
/// </summary>
public class AirlineQuery : PagedQuery
{
    public string? Country { get; init; }
    public bool? IsActive { get; init; }
}
