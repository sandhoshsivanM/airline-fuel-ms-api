using AirlineFuelMS.Core.Attributes;

namespace AirlineFuelMS.Core.Entities;

/// <summary>
/// A physical address (location) where a FuelProvider operates.
/// A single provider company may have many addresses across multiple countries.
/// </summary>
public class FuelProviderAddress
{
    public int Id { get; set; }
    public int FuelProviderId { get; set; }
    public int CountryId { get; set; }

    [Search] public string AddressLine1 { get; set; } = string.Empty;
    [Search] public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Marks the head-office / primary address for the provider.</summary>
    public bool IsHeadOffice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FuelProvider FuelProvider { get; set; } = null!;
    public Country Country { get; set; } = null!;
}
