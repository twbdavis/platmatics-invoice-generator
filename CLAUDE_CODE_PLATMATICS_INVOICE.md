# Claude Code Prompt: Platmatics Invoice Generator (C#)

## Goal

Build a C# console application that generates professional PDF invoices for Platmatics, a digital platform studio. The program reads invoice data from a JSON file and produces a clean, branded PDF matching the layout described below.

## Project Setup

Create a new .NET 8 console app in a folder called `PlatmaticsInvoice` at the current directory.

```
dotnet new console -n PlatmaticsInvoice
cd PlatmaticsInvoice
dotnet add package QuestPDF
dotnet add package System.Text.Json
```

QuestPDF requires a license setting. Add this line at the top of `Program.cs` before any document generation:

```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
```

## Project Structure

```
PlatmaticsInvoice/
├── PlatmaticsInvoice.csproj
├── Program.cs
├── Models/
│   └── Invoice.cs
├── Documents/
│   └── InvoiceDocument.cs
├── Assets/
│   └── (Thomas will place platmatics-logo.png here)
├── Output/
│   └── (generated PDFs land here)
└── sample-invoice.json
```

## Brand Colors

Use these exact hex values. Define them as static `Color` fields in the document class for reuse.

| Name | Hex | Usage |
|---|---|---|
| Primary Dark | `#1A1A2E` | Invoice badge background, table header, dividers |
| Accent Gold | `#C5A253` | "INVOICE" text in the badge, accent borders on the bill-to/issued-by boxes |
| Light Background | `#FAF8F5` | Amount Due box background, badge detail rows |
| Body Text | `#2D2D2D` | All body copy |
| Muted Text | `#6B7280` | Labels like "BILL TO", "ISSUED BY", secondary text |
| White | `#FFFFFF` | Page background, text on dark backgrounds |
| Border | `#E5E5E5` | Table cell borders, box outlines |

## Fonts

Use the system default sans-serif font (QuestPDF defaults to a clean sans). Do not add custom font files.

## Page Layout

- **Page size:** US Letter (8.5 x 11 inches)
- **Margins:** 60pt left, 60pt right, 50pt top, 40pt bottom
- **Orientation:** Portrait

## Invoice PDF Layout (Top to Bottom)

Replicate this structure precisely. Every section is described in render order.

### 1. Header Row

A single horizontal row with two elements pushed to opposite sides.

**Left side:** The Platmatics logo. Load from `Assets/platmatics-logo.png`. If the file does not exist, fall back to rendering "PLATMATICS" as bold text in Primary Dark at 28pt with "DIGITAL PLATFORM STUDIO" below it at 11pt in Muted Text. Set the logo image max height to 55pt, constrain width proportionally.

**Right side:** The invoice badge. This is a box with:
- Top section: Primary Dark (`#1A1A2E`) background, "INVOICE" text in Accent Gold (`#C5A253`), bold, 20pt, 10pt vertical padding, 15pt horizontal padding.
- Below that: Light Background (`#FAF8F5`) with three rows of key-value pairs, each row having the label left-aligned in Muted Text 9pt and the value right-aligned in Body Text 9pt bold. Rows:
  - "Invoice Number" | the invoice number
  - "Invoice Date" | the formatted date
  - "Status" | the status string
- The whole badge has a 1pt border in Border color with 4pt corner radius.
- Badge total width: approximately 220pt.

### 2. Divider

A horizontal line spanning the full content width. 1.5pt thick, Primary Dark color. Add 20pt vertical spacing above and below.

### 3. Bill To / Issued By Row

Two boxes side by side with equal width, separated by a 20pt gap.

Each box:
- 1pt border in Border color, 8pt corner radius.
- 20pt internal padding on all sides.
- A 3pt left border accent in Accent Gold (this is the distinguishing design touch).
- Header label: "BILL TO" or "ISSUED BY" in Muted Text, 8pt, bold, letter-spacing 1pt, all caps.
- 8pt spacing below the label.
- Name: Body Text, 13pt, bold.
- Address lines: Body Text, 10pt, normal weight. Each line on its own row.
- If email or phone is present, render those below the address in the same 10pt style.

### 4. Itemized Charges Section

20pt top spacing. Section header: "Itemized Charges" in Body Text, 11pt, bold. 10pt spacing below.

**Table:**
- Full content width.
- Header row: Primary Dark background, white text, 9pt, bold, all caps, 10pt vertical cell padding, 12pt horizontal cell padding.
- Column layout (left to right):
  - DESCRIPTION: takes remaining width (flexible), left-aligned
  - QTY: 70pt fixed width, center-aligned
  - UNIT PRICE: 100pt fixed width, right-aligned
  - AMOUNT: 100pt fixed width, right-aligned
- Data rows: alternating white and Light Background (`#FAF8F5`), Body Text 10pt, same cell padding as header. Bottom border 0.5pt in Border color on each row.
- Format currency values as `$X,XXX.XX` (USD, two decimal places, comma thousands separator).

### 5. Amount Due Box

Right-aligned below the table with 20pt top spacing. A box approximately 200pt wide:
- Light Background fill, 8pt corner radius.
- 2pt top border in Accent Gold.
- 20pt internal padding.
- "Amount Due" label: Muted Text, 10pt.
- Total amount: Body Text, 26pt, bold. Formatted as currency.

### 6. Footer

Pinned to the bottom of the page (use QuestPDF's footer slot). A 1pt horizontal line in Border color, then 8pt below: "Please remit payment to Platmatics and reference the invoice number above." in Muted Text, 8pt, centered.

## Data Model (Models/Invoice.cs)

```csharp
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
```

## Sample Invoice JSON (sample-invoice.json)

Create this file at the project root so the program can be tested immediately.

```json
{
  "invoiceNumber": "PLT-2026-0001",
  "invoiceDate": "2026-07-08",
  "status": "Payment due",
  "billTo": {
    "name": "Crowned Royalty Hair Institute",
    "addressLine1": "401 N Main St, Suite 111",
    "addressLine2": "Bryan, TX 77803",
    "email": "info@crownedroyaltyhairinstitute.net"
  },
  "issuedBy": {
    "name": "Platmatics",
    "addressLine1": "College Station, TX",
    "addressLine2": "",
    "email": "twbdavis@tamu.edu"
  },
  "items": [
    {
      "description": "Platform Development - Phase 1 (Website + Booking System)",
      "quantity": 1,
      "unitPrice": 1000.00
    },
    {
      "description": "Monthly Platform Maintenance - July 2026",
      "quantity": 1,
      "unitPrice": 55.00
    }
  ],
  "notes": null
}
```

## Program.cs (CLI Entry Point)

The program accepts one argument: the path to a JSON invoice file. If no argument is given, it defaults to `sample-invoice.json` in the current directory.

Flow:
1. Parse the command-line argument for the JSON file path.
2. Read and deserialize the JSON file into an `Invoice` object. Use `System.Text.Json` with `PropertyNameCaseInsensitive = true`.
3. Generate the invoice number for the output filename: sanitize the invoice number (replace non-alphanumeric chars with dashes) and use the pattern `Platmatics-Invoice-{sanitized}.pdf`.
4. Create the `Output/` directory if it does not exist.
5. Generate the PDF using the `InvoiceDocument` class and save it to `Output/{filename}`.
6. Print the output path to the console.

If the JSON file is not found or deserialization fails, print a clear error message and exit with code 1. Do not throw unhandled exceptions.

## InvoiceDocument.cs (QuestPDF Document)

This class implements `IDocument` and contains all rendering logic. Follow the layout spec above exactly. Key implementation notes:

- Use `Document.Create(container => { ... })` fluent API.
- Use `.Page()` with `.Size(PageSizes.Letter)` and the margin values specified.
- Use `.Header()`, `.Content()`, and `.Footer()` for the three page zones.
- The header contains sections 1-2 (logo row and divider).
- The content contains sections 3-5 (bill-to row, table, amount due).
- The footer contains section 6.
- For the Accent Gold left-border on the bill-to boxes: use a `.Row()` inside each box with a narrow colored column (3pt wide) on the left and the text content on the right. Or use `.BorderLeft(3).BorderColor(accentGold)` if QuestPDF supports per-side border colors.
- Use `.AlignRight()` for the invoice badge and amount due box.
- Format dates as "MMMM d, yyyy" (e.g., "July 8, 2026").
- For alternating row colors, use the row index modulo 2.

## Logo Handling

Check if `Assets/platmatics-logo.png` exists at runtime. If yes, load and render it. If no, render the text fallback described in section 1. Do not crash if the logo is missing.

The logo path should be resolved relative to the application's base directory, not the working directory. Use `AppContext.BaseDirectory` combined with a relative path, or accept a `--logo` command-line flag as an override.

Also: copy the Assets folder to the output on build. Add this to the `.csproj`:

```xml
<ItemGroup>
  <None Update="Assets\**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## After Building

1. Run `dotnet build` and fix any compilation errors.
2. Run `dotnet run -- sample-invoice.json` and confirm a PDF is generated in `Output/`.
3. Open the PDF and verify:
   - Logo or text fallback renders in the top left.
   - Invoice badge is top right with correct number, date, status.
   - Divider line spans the width.
   - Bill To and Issued By boxes are side by side with the gold left accent.
   - Table has the dark header row and correct column alignment.
   - Amount due box is right-aligned with the gold top accent.
   - Footer text is at the bottom.
   - All colors match the hex values specified.
4. If any element is misaligned or missing, fix it before finishing.

## Do NOT

- Do not deploy anything. This is a local tool.
- Do not add authentication or a database.
- Do not use any PDF library other than QuestPDF.
- Do not use external font files. System defaults are fine.
- Do not hardcode invoice data in the document class. All data comes from the Invoice model.
