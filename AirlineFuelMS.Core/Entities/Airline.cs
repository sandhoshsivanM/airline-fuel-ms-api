using AirlineFuelMS.Core.Attributes;

namespace AirlineFuelMS.Core.Entities;

public class Airline
{
    public int Id { get; set; }
    [Search] public string Name { get; set; } = string.Empty;     // e.g. "Emirate Airlines EK521"
    [Search] public string Code { get; set; } = string.Empty;     // e.g. "EK"

    /// <summary>Aircraft model — e.g. "Boeing 737-800", "Airbus A380-800".</summary>
    [Search] public string Model { get; set; } = string.Empty;

    /// <summary>Maximum passenger capacity for the aircraft model.</summary>
    public int PassengerCapacity { get; set; }

    /// <summary>Aircraft fuel tank capacity in liters — used for capacity validation on fueling.</summary>
    public int FuelTankCapacityLiters { get; set; }

    public string Country { get; set; } = string.Empty;           // base / country of registration
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
