namespace AirlineFuelMS.Core.Entities;

public class FuelTransaction
{
    public int Id { get; set; }
    public int AirlineId { get; set; }
    public int FuelProviderId { get; set; }

    /// <summary>
    /// The specific provider address (location) where the fueling happened.
    /// Determines the country and therefore the currency used on the invoice.
    /// </summary>
    public int FuelProviderAddressId { get; set; }

    public int CreatedByUserId { get; set; }
    public decimal QuantityLiters { get; set; }
    public decimal PricePerLiter { get; set; }
    public decimal TotalAmount => QuantityLiters * PricePerLiter;
    public string TransactionRef { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public Airline Airline { get; set; } = null!;
    public FuelProvider FuelProvider { get; set; } = null!;
    public FuelProviderAddress FuelProviderAddress { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
