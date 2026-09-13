using PlatmaticsInvoice.Documents;
using PlatmaticsInvoice.Models;
using QuestPDF.Fluent;

public class InvoiceTests
{
    [Theory]
    [InlineData("oops", "125")]
    [InlineData("0", "125")]
    [InlineData("-1", "125")]
    [InlineData("1.5", "125")]
    [InlineData("1", "oops")]
    [InlineData("1", "-0.01")]
    [InlineData("1", "1.234")]
    [InlineData("2", "79228162514264337593543950335")]
    public void Invalid_input_cannot_become_an_invoice_line(string quantity, string price)
    {
        Assert.False(InvoiceInput.TryReadItem("Consultation", quantity, price, out var item, out var error));
        Assert.Null(item);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Currency_and_quantity_produce_the_expected_amount()
    {
        Assert.True(InvoiceInput.TryReadItem(" Consultation ", "2", "$125.50", out var item, out _));
        Assert.Equal(251m, item!.Amount);
        Assert.Equal("Consultation", item.Description);
        Assert.True(InvoiceInput.TryReadItem("Complimentary", "1", "0", out _, out _));
    }

    [Fact]
    public void Empty_description_is_rejected()
        => Assert.False(InvoiceInput.TryReadItem(" ", "1", "10", out _, out _));

    [Fact]
    public void Long_invoice_generates_a_pdf_without_a_logo()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var invoice = new Invoice
        {
            InvoiceNumber = "EXAMPLE-0001", InvoiceDate = new DateTime(2026, 9, 12),
            BillTo = new BillingParty { Name = "Example Customer" },
            IssuedBy = new BillingParty { Name = "Example Business" },
            Items = Enumerable.Range(1, 60).Select(i => new InvoiceItem
            { Description = $"Consultation item {i}: planning and implementation", Quantity = 2, UnitPrice = 125.50m }).ToList()
        };
        var pdf = new InvoiceDocument(invoice, null).GeneratePdf();
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
