using System.Globalization;
using PlatmaticsInvoice.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Color = QuestPDF.Infrastructure.Color;

namespace PlatmaticsInvoice.Documents;

public class InvoiceDocument : IDocument
{
    private static readonly Color PrimaryDark = Color.FromHex("#111111");
    private static readonly Color AccentDark = Color.FromHex("#111111");
    private static readonly Color LightBackground = Color.FromHex("#F5F5F5");
    private static readonly Color BodyText = Color.FromHex("#2D2D2D");
    private static readonly Color MutedText = Color.FromHex("#6B6B6B");
    private static readonly Color White = Color.FromHex("#FFFFFF");
    private static readonly Color Border = Color.FromHex("#E5E5E5");

    private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

    private readonly Invoice _invoice;
    private readonly string? _logoPath;

    public InvoiceDocument(Invoice invoice, string? logoPath)
    {
        _invoice = invoice;
        _logoPath = logoPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.MarginLeft(60);
            page.MarginRight(60);
            page.MarginTop(50);
            page.MarginBottom(40);
            page.PageColor(White);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor(BodyText));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().AlignLeft().AlignMiddle().Element(ComposeLogo);
                row.ConstantItem(220).AlignRight().Element(ComposeInvoiceBadge);
            });

            // Divider
            column.Item().PaddingVertical(20).LineHorizontal(1.5f).LineColor(PrimaryDark);
        });
    }

    private void ComposeLogo(IContainer container)
    {
        if (_logoPath is not null && File.Exists(_logoPath))
        {
            container.MaxHeight(55).Image(_logoPath).FitHeight();
        }
        else
        {
            container.Column(column =>
            {
                column.Item().Text("PLATMATICS").FontSize(28).Bold().FontColor(PrimaryDark);
                column.Item().Text("DIGITAL PLATFORM STUDIO").FontSize(11).FontColor(MutedText);
            });
        }
    }

    private void ComposeInvoiceBadge(IContainer container)
    {
        container
            .Border(1)
            .BorderColor(Border)
            .CornerRadius(4)
            .Column(column =>
            {
                column.Item()
                    .Background(PrimaryDark)
                    .PaddingVertical(10)
                    .PaddingHorizontal(15)
                    .Text("INVOICE")
                    .FontSize(20)
                    .Bold()
                    .FontColor(White);

                column.Item().Background(LightBackground).PaddingVertical(6).PaddingHorizontal(15).Column(details =>
                {
                    details.Item().Element(c => BadgeDetailRow(c, "Invoice Number", _invoice.InvoiceNumber));
                    details.Item().Element(c => BadgeDetailRow(c, "Invoice Date", _invoice.InvoiceDate.ToString("MMMM d, yyyy", Usd)));
                    details.Item().Element(c => BadgeDetailRow(c, "Status", _invoice.Status));
                });
            });
    }

    private static void BadgeDetailRow(IContainer container, string label, string value)
    {
        container.PaddingVertical(3).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9).FontColor(MutedText);
            row.RelativeItem().AlignRight().Text(value).FontSize(9).Bold().FontColor(BodyText);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => ComposePartyBox(c, "BILL TO", _invoice.BillTo));
                row.ConstantItem(20);
                row.RelativeItem().Element(c => ComposePartyBox(c, "ISSUED BY", _invoice.IssuedBy));
            });

            column.Item().PaddingTop(20).Text("Itemized Charges").FontSize(11).Bold().FontColor(BodyText);
            column.Item().PaddingTop(10).Element(ComposeItemsTable);

            column.Item().PaddingTop(20).AlignRight().Element(ComposeAmountDue);
        });
    }

    private static void ComposePartyBox(IContainer container, string label, BillingParty party)
    {
        container
            .Border(1)
            .BorderColor(Border)
            .CornerRadius(8)
            .Row(row =>
            {
                row.ConstantItem(3).Background(AccentDark);
                row.RelativeItem().Padding(20).Column(column =>
                {
                    column.Item().Text(label.ToUpperInvariant()).FontSize(8).Bold().FontColor(MutedText).LetterSpacing(0.125f);
                    column.Item().PaddingTop(8).Text(party.Name).FontSize(13).Bold().FontColor(BodyText);

                    if (!string.IsNullOrWhiteSpace(party.AddressLine1))
                        column.Item().Text(party.AddressLine1).FontSize(10).FontColor(BodyText);
                    if (!string.IsNullOrWhiteSpace(party.AddressLine2))
                        column.Item().Text(party.AddressLine2).FontSize(10).FontColor(BodyText);
                    if (!string.IsNullOrWhiteSpace(party.Email))
                        column.Item().Text(party.Email).FontSize(10).FontColor(BodyText);
                    if (!string.IsNullOrWhiteSpace(party.Phone))
                        column.Item().Text(party.Phone).FontSize(10).FontColor(BodyText);
                });
            });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(70);
                columns.ConstantColumn(100);
                columns.ConstantColumn(100);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("DESCRIPTION");
                header.Cell().Element(HeaderCell).AlignCenter().Text("QTY");
                header.Cell().Element(HeaderCell).AlignRight().Text("UNIT PRICE");
                header.Cell().Element(HeaderCell).AlignRight().Text("AMOUNT");
            });

            for (var i = 0; i < _invoice.Items.Count; i++)
            {
                var item = _invoice.Items[i];
                var background = i % 2 == 0 ? White : LightBackground;

                table.Cell().Element(c => DataCell(c, background)).Text(item.Description);
                table.Cell().Element(c => DataCell(c, background)).AlignCenter().Text(item.Quantity.ToString(Usd));
                table.Cell().Element(c => DataCell(c, background)).AlignRight().Text(FormatCurrency(item.UnitPrice));
                table.Cell().Element(c => DataCell(c, background)).AlignRight().Text(FormatCurrency(item.Amount));
            }
        });

        static IContainer HeaderCell(IContainer container) => container
            .Background(PrimaryDark)
            .PaddingVertical(10)
            .PaddingHorizontal(12)
            .DefaultTextStyle(style => style.FontSize(9).Bold().FontColor(White));

        static IContainer DataCell(IContainer container, Color background) => container
            .Background(background)
            .BorderBottom(0.5f)
            .BorderColor(Border)
            .PaddingVertical(10)
            .PaddingHorizontal(12)
            .DefaultTextStyle(style => style.FontSize(10).FontColor(BodyText));
    }

    private void ComposeAmountDue(IContainer container)
    {
        var total = _invoice.Items.Sum(item => item.Amount);

        container
            .Width(200)
            .Background(LightBackground)
            .CornerRadius(8)
            .BorderTop(2)
            .BorderColor(AccentDark)
            .Padding(20)
            .Column(column =>
            {
                column.Item().Text("Amount Due").FontSize(10).FontColor(MutedText);
                column.Item().Text(FormatCurrency(total)).FontSize(26).Bold().FontColor(BodyText);
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Border);
            column.Item().PaddingTop(8).AlignCenter()
                .Text("Please remit payment to Platmatics and reference the invoice number above.")
                .FontSize(8)
                .FontColor(MutedText);
        });
    }

    private static string FormatCurrency(decimal value) => value.ToString("C2", Usd);
}
