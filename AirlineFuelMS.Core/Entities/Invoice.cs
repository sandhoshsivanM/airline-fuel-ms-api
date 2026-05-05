namespace AirlineFuelMS.Core.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int FuelTransactionId { get; set; }
    public int AirlineId { get; set; }
    public int FuelProviderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;   // e.g. "INV-20240501-001"
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Unpaid";              // Unpaid | Paid | Overdue
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public FuelTransaction FuelTransaction { get; set; } = null!;
    public Airline Airline { get; set; } = null!;
    public FuelProvider FuelProvider { get; set; } = null!;
}
