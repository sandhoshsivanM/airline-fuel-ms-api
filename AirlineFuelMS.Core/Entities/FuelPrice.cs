namespace AirlineFuelMS.Core.Entities;

public class FuelPrice
{
    public int Id { get; set; }
    public int FuelProviderId { get; set; }
    public decimal PricePerLiter { get; set; }             // e.g. 0.58 for Emirate
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public FuelProvider FuelProvider { get; set; } = null!;
}
