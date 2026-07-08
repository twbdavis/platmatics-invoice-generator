using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlatmaticsInvoice.Documents;
using PlatmaticsInvoice.Models;
using QuestPDF.Fluent;

namespace PlatmaticsInvoice.Forms;

public class MainForm : Form
{
    private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

    private readonly TextBox _invoiceNumber = new();
    private readonly DateTimePicker _invoiceDate = new();
    private readonly ComboBox _status = new();

    private readonly TextBox _billToName = new();
    private readonly TextBox _billToAddress1 = new();
    private readonly TextBox _billToAddress2 = new();
    private readonly TextBox _billToEmail = new();
    private readonly TextBox _billToPhone = new();

    private readonly TextBox _issuedByName = new();
    private readonly TextBox _issuedByAddress1 = new();
    private readonly TextBox _issuedByAddress2 = new();
    private readonly TextBox _issuedByEmail = new();
    private readonly TextBox _issuedByPhone = new();

    private readonly DataGridView _items = new();
    private readonly Label _totalLabel = new();
    private readonly CheckBox _openAfterSave = new();
    private readonly Button _generateButton = new();

    public MainForm()
    {
        Text = "Platmatics Invoice Generator";
        MinimumSize = new Size(860, 720);
        ClientSize = new Size(920, 740);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "platmatics.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        BuildLayout();
        LoadDefaults();
        RecalculateAmounts();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 4,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // --- Invoice details (spans both columns) ---
        var detailsGroup = new GroupBox { Text = "Invoice Details", Dock = DockStyle.Fill, Padding = new Padding(12) };
        var detailsTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, AutoSize = true };
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        _invoiceDate.Format = DateTimePickerFormat.Long;
        _status.DropDownStyle = ComboBoxStyle.DropDown;
        _status.Items.AddRange(new object[] { "Payment due", "Paid", "Overdue", "Draft" });

        detailsTable.Controls.Add(FieldLabel("Invoice Number"), 0, 0);
        detailsTable.Controls.Add(Stretch(_invoiceNumber), 1, 0);
        detailsTable.Controls.Add(FieldLabel("Invoice Date"), 2, 0);
        detailsTable.Controls.Add(Stretch(_invoiceDate), 3, 0);
        detailsTable.Controls.Add(FieldLabel("Status"), 4, 0);
        detailsTable.Controls.Add(Stretch(_status), 5, 0);
        detailsGroup.Controls.Add(detailsTable);
        root.Controls.Add(detailsGroup, 0, 0);
        root.SetColumnSpan(detailsGroup, 2);

        // --- Bill To / Issued By ---
        var billToGroup = BuildPartyGroup("Bill To", _billToName, _billToAddress1, _billToAddress2, _billToEmail, _billToPhone);
        billToGroup.Margin = new Padding(0, 8, 8, 0);
        root.Controls.Add(billToGroup, 0, 1);

        var issuedByGroup = BuildPartyGroup("Issued By", _issuedByName, _issuedByAddress1, _issuedByAddress2, _issuedByEmail, _issuedByPhone);
        issuedByGroup.Margin = new Padding(8, 8, 0, 0);
        root.Controls.Add(issuedByGroup, 1, 1);

        // --- Line items ---
        var itemsGroup = new GroupBox { Text = "Line Items", Dock = DockStyle.Fill, Padding = new Padding(12), Margin = new Padding(0, 8, 0, 0) };
        ConfigureItemsGrid();
        var itemsHint = new Label
        {
            Text = "Type in the bottom row to add an item. Select a row and press Delete to remove it.",
            Dock = DockStyle.Bottom,
            ForeColor = Color.DimGray,
            AutoSize = false,
            Height = 22,
        };
        itemsGroup.Controls.Add(_items);
        itemsGroup.Controls.Add(itemsHint);
        root.Controls.Add(itemsGroup, 0, 2);
        root.SetColumnSpan(itemsGroup, 2);

        // --- Bottom bar ---
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _totalLabel.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        _totalLabel.TextAlign = ContentAlignment.MiddleLeft;
        _totalLabel.AutoSize = true;
        _totalLabel.Anchor = AnchorStyles.Left;

        _openAfterSave.Text = "Open PDF after saving";
        _openAfterSave.Checked = true;
        _openAfterSave.AutoSize = true;
        _openAfterSave.Anchor = AnchorStyles.Right;
        _openAfterSave.Margin = new Padding(0, 0, 16, 0);

        _generateButton.Text = "Generate PDF…";
        _generateButton.AutoSize = true;
        _generateButton.Padding = new Padding(14, 6, 14, 6);
        _generateButton.Anchor = AnchorStyles.Right;
        _generateButton.Click += (_, _) => GeneratePdf();

        bottom.Controls.Add(_totalLabel, 0, 0);
        bottom.Controls.Add(_openAfterSave, 1, 0);
        bottom.Controls.Add(_generateButton, 2, 0);
        root.Controls.Add(bottom, 0, 3);
        root.SetColumnSpan(bottom, 2);

        Controls.Add(root);
        AcceptButton = _generateButton;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 8, 0),
    };

    private static Control Stretch(Control control)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 2, 12, 2);
        return control;
    }

    private static GroupBox BuildPartyGroup(string title, TextBox name, TextBox address1, TextBox address2, TextBox email, TextBox phone)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(12), AutoSize = true };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var rows = new (string Label, TextBox Box)[]
        {
            ("Name", name),
            ("Address Line 1", address1),
            ("Address Line 2", address2),
            ("Email", email),
            ("Phone", phone),
        };

        for (var i = 0; i < rows.Length; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(FieldLabel(rows[i].Label), 0, i);
            table.Controls.Add(Stretch(rows[i].Box), 1, i);
        }

        group.Controls.Add(table);
        return group;
    }

    private void ConfigureItemsGrid()
    {
        _items.Dock = DockStyle.Fill;
        _items.AllowUserToAddRows = true;
        _items.AllowUserToDeleteRows = true;
        _items.RowHeadersWidth = 28;
        _items.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _items.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _items.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _items.BackgroundColor = SystemColors.Window;

        var description = new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 60 };
        var qty = new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", FillWeight = 10 };
        var unitPrice = new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Unit Price", FillWeight = 15 };
        var amount = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", FillWeight = 15, ReadOnly = true };

        qty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        unitPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        amount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        amount.DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

        _items.Columns.AddRange(description, qty, unitPrice, amount);

        _items.CellValueChanged += (_, _) => RecalculateAmounts();
        _items.RowsRemoved += (_, _) => RecalculateAmounts();
        _items.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_items.IsCurrentCellDirty)
                _items.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _items.DataError += (_, e) => e.ThrowException = false;
    }

    private void LoadDefaults()
    {
        _invoiceDate.Value = DateTime.Today;
        _status.Text = "Payment due";

        var defaults = ReadDefaultsFile();

        if (defaults is not null)
        {
            _issuedByName.Text = defaults.IssuedBy.Name;
            _issuedByAddress1.Text = defaults.IssuedBy.AddressLine1;
            _issuedByAddress2.Text = defaults.IssuedBy.AddressLine2;
            _issuedByEmail.Text = defaults.IssuedBy.Email ?? string.Empty;
            _issuedByPhone.Text = defaults.IssuedBy.Phone ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(defaults.Status))
                _status.Text = defaults.Status;
        }

        _invoiceNumber.Text = $"PLT-{DateTime.Today:yyyy}-";
        _items.Rows.Clear();
    }

    private Invoice? ReadDefaultsFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "sample-invoice.json");
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Invoice>(File.ReadAllText(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private void RecalculateAmounts()
    {
        var total = 0m;

        foreach (DataGridViewRow row in _items.Rows)
        {
            if (row.IsNewRow)
                continue;

            var qty = ParseInt(row.Cells["Qty"].Value);
            var price = ParseDecimal(row.Cells["UnitPrice"].Value);
            var amount = qty * price;
            total += amount;

            row.Cells["Amount"].Value = amount.ToString("C2", Usd);
        }

        _totalLabel.Text = $"Amount Due: {total.ToString("C2", Usd)}";
    }

    private static int ParseInt(object? value)
    {
        if (value is null) return 0;
        return int.TryParse(value.ToString(), NumberStyles.Integer | NumberStyles.AllowThousands, Usd, out var result) ? result : 0;
    }

    private static decimal ParseDecimal(object? value)
    {
        if (value is null) return 0m;
        var text = value.ToString()?.Replace("$", "").Trim() ?? string.Empty;
        return decimal.TryParse(text, NumberStyles.Number, Usd, out var result) ? result : 0m;
    }

    private Invoice? CollectInvoice(out string? validationError)
    {
        validationError = null;

        var invoice = new Invoice
        {
            InvoiceNumber = _invoiceNumber.Text.Trim(),
            InvoiceDate = _invoiceDate.Value.Date,
            Status = _status.Text.Trim(),
            BillTo = new BillingParty
            {
                Name = _billToName.Text.Trim(),
                AddressLine1 = _billToAddress1.Text.Trim(),
                AddressLine2 = _billToAddress2.Text.Trim(),
                Email = NullIfEmpty(_billToEmail.Text),
                Phone = NullIfEmpty(_billToPhone.Text),
            },
            IssuedBy = new BillingParty
            {
                Name = _issuedByName.Text.Trim(),
                AddressLine1 = _issuedByAddress1.Text.Trim(),
                AddressLine2 = _issuedByAddress2.Text.Trim(),
                Email = NullIfEmpty(_issuedByEmail.Text),
                Phone = NullIfEmpty(_issuedByPhone.Text),
            },
        };

        foreach (DataGridViewRow row in _items.Rows)
        {
            if (row.IsNewRow)
                continue;

            var description = row.Cells["Description"].Value?.ToString()?.Trim() ?? string.Empty;
            var qty = ParseInt(row.Cells["Qty"].Value);
            var price = ParseDecimal(row.Cells["UnitPrice"].Value);

            if (description.Length == 0 && qty == 0 && price == 0m)
                continue;

            if (description.Length == 0)
            {
                validationError = "Every line item needs a description.";
                return null;
            }

            invoice.Items.Add(new InvoiceItem
            {
                Description = description,
                Quantity = qty == 0 ? 1 : qty,
                UnitPrice = price,
            });
        }

        if (invoice.InvoiceNumber.Length == 0 || invoice.InvoiceNumber.EndsWith('-'))
            validationError = "Enter a complete invoice number (e.g. PLT-2026-0001).";
        else if (invoice.BillTo.Name.Length == 0)
            validationError = "Enter a Bill To name.";
        else if (invoice.IssuedBy.Name.Length == 0)
            validationError = "Enter an Issued By name.";
        else if (invoice.Items.Count == 0)
            validationError = "Add at least one line item.";

        return validationError is null ? invoice : null;
    }

    private static string? NullIfEmpty(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private void GeneratePdf()
    {
        var invoice = CollectInvoice(out var validationError);
        if (invoice is null)
        {
            MessageBox.Show(this, validationError, "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var sanitized = Regex.Replace(invoice.InvoiceNumber, "[^a-zA-Z0-9]+", "-").Trim('-');
        if (sanitized.Length == 0)
            sanitized = "invoice";

        using var dialog = new SaveFileDialog
        {
            Title = "Save Invoice PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"Platmatics-Invoice-{sanitized}.pdf",
            DefaultExt = "pdf",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "platmatics-logo.png");

        try
        {
            new InvoiceDocument(invoice, logoPath).GeneratePdf(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"PDF generation failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_openAfterSave.Checked)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch
            {
                // The PDF was saved; failing to auto-open is not worth an error dialog.
            }
        }
    }
}
