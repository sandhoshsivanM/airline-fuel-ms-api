namespace AirlineFuelMS.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "NormalUser"; // "Admin" | "NormalUser"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FuelTransaction> FuelTransactions { get; set; } = new List<FuelTransaction>();
}
