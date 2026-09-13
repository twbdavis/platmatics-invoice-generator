using System.Globalization;

namespace PlatmaticsInvoice.Models;

public static class InvoiceInput
{
    private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

    public static bool TryReadItem(string? description, string? quantityText, string? priceText,
        out InvoiceItem? item, out string? error)
    {
        item = null;
        error = null;
        if (string.IsNullOrWhiteSpace(description))
            error = "Enter a description.";
        else if (!int.TryParse(quantityText, NumberStyles.Integer | NumberStyles.AllowThousands, Usd, out var quantity) || quantity <= 0)
            error = "Quantity must be a whole number greater than zero.";
        else if (!decimal.TryParse(priceText?.Trim().TrimStart('$'), NumberStyles.Number, Usd, out var price) || price < 0)
            error = "Unit price must be a number of zero or more (for example, 125.00).";
        else if (decimal.Round(price, 2) != price)
            error = "Unit price must have no more than two decimal places.";
        else
        {
            try
            {
                _ = checked(quantity * price);
                item = new InvoiceItem { Description = description.Trim(), Quantity = quantity, UnitPrice = price };
            }
            catch (OverflowException)
            {
                error = "The line amount is too large. Reduce the quantity or unit price.";
            }
        }
        return item is not null;
    }
}
