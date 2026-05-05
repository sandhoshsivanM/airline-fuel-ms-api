using AirlineFuelMS.Core.Attributes;

namespace AirlineFuelMS.Core.Entities;

public class FuelProvider
{
    public int Id { get; set; }
    [Search] public string Name { get; set; } = string.Empty;       // e.g. "Hindustan Petroleum"
    [Search] public string Code { get; set; } = string.Empty;       // e.g. "HP", "BP", "EMI"
    public string ContactInfo { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// A provider company can have addresses in multiple countries.
    /// Country information lives on the address, not on the provider itself.
    /// </summary>
    public ICollection<FuelProviderAddress> Addresses { get; set; } = new List<FuelProviderAddress>();
    public ICollection<FuelPrice> FuelPrices { get; set; } = new List<FuelPrice>();
    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
