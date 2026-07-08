namespace PlatmaticsInvoice.Models;

public class Invoice
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Payment due";

    public BillingParty BillTo { get; set; } = new();
    public BillingParty IssuedBy { get; set; } = new();

    public List<InvoiceItem> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public class BillingParty
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class InvoiceItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
}
