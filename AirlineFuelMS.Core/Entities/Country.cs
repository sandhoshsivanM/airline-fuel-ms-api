using AirlineFuelMS.Core.Attributes;

namespace AirlineFuelMS.Core.Entities;

public class Country
{
    public int Id { get; set; }
    [Search] public string Name { get; set; } = string.Empty;   // e.g. "India"
    [Search] public string Code { get; set; } = string.Empty;   // ISO-2 country code: "IN"

    /// <summary>ISO-4217 currency code: "INR", "AED", "SGD".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Glyph for display: "₹", "د.إ", "S$".</summary>
    public string CurrencySymbol { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<FuelProviderAddress> Addresses { get; set; } = new List<FuelProviderAddress>();
}
