using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace ErpMobile.Pages;

/// <summary>
/// صفحة عرض تفاصيل فاتورة محفوظة مع الإمكانية <b>تعديلها وحفظها</b>.
/// تُفتح من قائمة الفواتير بالضغط على أي فاتورة (بدل الحوار البسيط القديم).
/// </summary>
[QueryProperty(nameof(InvoiceNumber), "num")]
public partial class InvoiceDetailsPage : ContentPage
{
    private const string ChooseItem = "-- اختر --";

    private readonly List<ItemRow> _rows = [];

    private Invoice? _invoice;
    private bool _ready;
    private bool _saving;

    private sealed class ItemRow
    {
        public InvoiceItem Item { get; init; } = null!;
        public Border RowView { get; init; } = null!;
        public Entry QuantityEntry { get; init; } = null!;
        public Entry PriceEntry { get; init; } = null!;
        public Entry DiscountPercentEntry { get; init; } = null!;
        public Label DiscountAmountLabel { get; init; } = null!;
        public Label NetLabel { get; init; } = null!;
        public Label IndexLabel { get; init; } = null!;
    }

    /// <summary>رقم الفاتورة — يأتي من الرابط (QueryProperty).</summary>
    public int InvoiceNumber { get; set; }

    public InvoiceDetailsPage()
    {
        InitializeComponent();

        InvoiceTypePicker.ItemsSource = new[] { "نقدي", "آجل", "تقسيط" };
        PriceListPicker.ItemsSource = new[] { ChooseItem, "سعر الجمهور", "سعر الجملة", "سعر التجزئة" };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_invoice is null)
        {
            await LoadAsync();
        }
    }

    // ---------------------------------------------------------------------
    // التحميل
    // ---------------------------------------------------------------------

    private async Task LoadAsync()
    {
        try
        {
            Invoice? invoice = await InvoiceRepository.Instance.GetByNumberAsync(InvoiceNumber);
            if (invoice is null)
            {
                await DisplayAlertAsync("غير موجودة", $"لم يتم العثور على الفاتورة رقم {InvoiceNumber}.", "موافق");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _invoice = invoice;
            await BindAsync(invoice);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("خطأ", $"تعذّر تحميل الفاتورة: {ex.Message}", "موافق");
        }
    }

    private async Task BindAsync(Invoice invoice)
    {
        _ready = false;

        TitleLabel.Text = $"فاتورة #{invoice.InvoiceNumber}";
        SubtitleLabel.Text = invoice.Customer;
        InvoiceNumberEntry.Text = invoice.InvoiceNumber.ToString(CultureInfo.InvariantCulture);

        CustomerEntry.Text = invoice.Customer;

        if (DateTime.TryParse(invoice.InvoiceDate, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out DateTime date))
        {
            InvoiceDatePicker.Date = date;
        }
        else
        {
            InvoiceDatePicker.Date = DateTime.Now;
        }

        InvoiceTypePicker.SelectedItem = invoice.InvoiceType;
        if (string.IsNullOrWhiteSpace(invoice.InvoiceType))
        {
            InvoiceTypePicker.SelectedIndex = 0;
        }

        int priceIndex = Array.IndexOf((string[])PriceListPicker.ItemsSource!, invoice.PriceList);
        PriceListPicker.SelectedIndex = priceIndex >= 0 ? priceIndex : 0;

        PaidEntry.Text = invoice.Paid.ToString("0.##", CultureInfo.InvariantCulture);

        // نسبة الخصم والضريبة المخزنتين نحسبهما عكسياً من القيم المحفوظة
        double baseBeforeInvoiceDiscount = invoice.Subtotal - invoice.ItemDiscount;
        double discountPercent = baseBeforeInvoiceDiscount > 0.0001
            ? invoice.InvoiceDiscount / baseBeforeInvoiceDiscount * 100d
            : 0d;

        double vatBase = baseBeforeInvoiceDiscount - invoice.InvoiceDiscount;
        double vatPercent = vatBase > 0.0001 ? invoice.Vat / vatBase * 100d : 0d;

        InvoiceDiscountEntry.Text = discountPercent.ToString("0.##", CultureInfo.InvariantCulture);
        VatPercentEntry.Text = vatPercent.ToString("0.##", CultureInfo.InvariantCulture);

        // البنود
        ItemsContainer.Children.Clear();
        _rows.Clear();

        List<InvoiceItem> items = await InvoiceRepository.Instance.GetItemsAsync(invoice.Id);

        EmptyLabel.IsVisible = items.Count == 0;

        for (int index = 0; index < items.Count; index++)
        {
            ItemRow row = BuildItemRow(items[index], index + 1);
            _rows.Add(row);
            ItemsContainer.Children.Add(row.RowView);
        }

        _ready = true;
        Recalculate();
    }

    // ---------------------------------------------------------------------
    // صفوف البنود
    // ---------------------------------------------------------------------

    private ItemRow BuildItemRow(InvoiceItem item, int number)
    {
        var indexLabel = new Label
        {
            Text = number.ToString(CultureInfo.InvariantCulture),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#6B62C7"),
            Padding = new Thickness(8, 3),
            VerticalOptions = LayoutOptions.Center
        };

        var nameLabel = new Label
        {
            Text = item.Name,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };

        var metaLabel = new Label
        {
            Text = $"{item.Barcode}  •  {item.Unit}",
            FontSize = 10,
            TextColor = Color.FromArgb("#8A91A6"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };

        var info = new VerticalStackLayout { Spacing = 1, Children = { nameLabel, metaLabel } };

        var netLabel = new Label
        {
            Text = item.Total.ToString("0.00", CultureInfo.InvariantCulture),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#173B70"),
            VerticalOptions = LayoutOptions.Center
        };

        var deleteButton = new Button
        {
            Text = "✕",
            FontSize = 11,
            BackgroundColor = Color.FromArgb("#D9534F"),
            TextColor = Colors.White,
            Padding = 0,
            WidthRequest = 36,
            HeightRequest = 32,
            CornerRadius = 6,
            VerticalOptions = LayoutOptions.Center
        };

        var headGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star),
                new(GridLength.Auto),
                new(GridLength.Auto)
            },
            ColumnSpacing = 8,
            FlowDirection = FlowDirection.RightToLeft
        };
        headGrid.Children.Add(indexLabel);
        headGrid.Children.Add(info);
        headGrid.Children.Add(netLabel);
        headGrid.Children.Add(deleteButton);
        Grid.SetColumn(indexLabel, 0);
        Grid.SetColumn(info, 1);
        Grid.SetColumn(netLabel, 2);
        Grid.SetColumn(deleteButton, 3);

        var quantityEntry = MakeFieldEntry(item.Quantity.ToString("0.##", CultureInfo.InvariantCulture), 70);
        var priceEntry = MakeFieldEntry(item.Price.ToString("0.##", CultureInfo.InvariantCulture), 88);
        var discountEntry = MakeFieldEntry(
            item.DiscountPercent.ToString("0.##", CultureInfo.InvariantCulture), 64);

        var discountAmountLabel = MakeValueLabel(
            item.DiscountAmount.ToString("0.00", CultureInfo.InvariantCulture), "#C77A11");

        quantityEntry.TextChanged += OnEditableChanged;
        priceEntry.TextChanged += OnEditableChanged;
        discountEntry.TextChanged += OnEditableChanged;

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Auto),
                new(GridLength.Auto),
                new(GridLength.Star)
            },
            ColumnSpacing = 10,
            RowSpacing = 4,
            FlowDirection = FlowDirection.RightToLeft
        };
        var quantityField = MakeLabeledField("الكمية", quantityEntry);
        var priceField = MakeLabeledField("السعر", priceEntry);
        var discountField = MakeLabeledField("خصم %", discountEntry);
        var discountAmountField = MakeLabeledValue("مبلغ الخصم", discountAmountLabel);

        fields.Children.Add(quantityField);
        fields.Children.Add(priceField);
        fields.Children.Add(discountField);
        fields.Children.Add(discountAmountField);
        Grid.SetColumn(quantityField, 0);
        Grid.SetColumn(priceField, 1);
        Grid.SetColumn(discountField, 2);
        Grid.SetColumn(discountAmountField, 3);

        var content = new VerticalStackLayout { Spacing = 8, Children = { headGrid, fields } };

        var border = new Border
        {
            Padding = new Thickness(10, 8),
            BackgroundColor = Color.FromArgb("#FBFCFE"),
            Stroke = Color.FromArgb("#DDE4F0"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = content
        };

        deleteButton.Clicked += (_, _) =>
        {
            ItemsContainer.Children.Remove(border);
            _rows.RemoveAll(r => r.QuantityEntry == quantityEntry);
            EmptyLabel.IsVisible = _rows.Count == 0;
            Renumber();
            Recalculate();
        };

        return new ItemRow
        {
            Item = item,
            QuantityEntry = quantityEntry,
            PriceEntry = priceEntry,
            DiscountPercentEntry = discountEntry,
            DiscountAmountLabel = discountAmountLabel,
            NetLabel = netLabel,
            IndexLabel = indexLabel,
            RowView = border
        };
    }

    private static Entry MakeFieldEntry(string text, double width) => new()
    {
        Text = text,
        WidthRequest = width,
        FontSize = 12,
        Keyboard = Keyboard.Numeric,
        BackgroundColor = Colors.White,
        TextColor = Color.FromArgb("#34445C"),
        HeightRequest = 36,
        HorizontalTextAlignment = TextAlignment.Center
    };

    private static Label MakeValueLabel(string text, string color) => new()
    {
        Text = text,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb(color),
        VerticalOptions = LayoutOptions.Center
    };

    private static VerticalStackLayout MakeLabeledField(string caption, View control)
    {
        var label = new Label
        {
            Text = caption,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6B7488")
        };

        return new VerticalStackLayout { Spacing = 2, Children = { label, control } };
    }

    private static VerticalStackLayout MakeLabeledValue(string caption, View control)
    {
        var label = new Label
        {
            Text = caption,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6B7488"),
            HorizontalOptions = LayoutOptions.Start
        };

        return new VerticalStackLayout { Spacing = 2, Children = { label, control } };
    }

    private void Renumber()
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            _rows[index].IndexLabel.Text = (index + 1).ToString(CultureInfo.InvariantCulture);
        }
    }

    // ---------------------------------------------------------------------
    // الحسابات
    // ---------------------------------------------------------------------

    private static double ParseAmount(string? text)
        => double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
            ? value
            : 0d;

    private void OnEditableChanged(object? sender, TextChangedEventArgs e) => Recalculate();

    private void Recalculate()
    {
        if (!_ready || GrandTotalLabel is null)
        {
            return;
        }

        double subtotal = 0;
        double itemsDiscount = 0;

        foreach (ItemRow row in _rows)
        {
            double quantity = ParseAmount(row.QuantityEntry.Text);
            double price = ParseAmount(row.PriceEntry.Text);
            double percent = Math.Clamp(ParseAmount(row.DiscountPercentEntry.Text), 0, 100);

            double gross = quantity * price;
            double discount = gross * percent / 100d;

            subtotal += gross;
            itemsDiscount += discount;

            row.DiscountAmountLabel.Text = discount.ToString("0.00", CultureInfo.InvariantCulture);
            row.NetLabel.Text = (gross - discount).ToString("0.00", CultureInfo.InvariantCulture);
        }

        double invoiceDiscountPercent = Math.Clamp(ParseAmount(InvoiceDiscountEntry.Text), 0, 100);
        double invoiceDiscount = (subtotal - itemsDiscount) * invoiceDiscountPercent / 100d;

        double vatPercent = Math.Clamp(ParseAmount(VatPercentEntry.Text), 0, 100);
        double vat = (subtotal - itemsDiscount - invoiceDiscount) * vatPercent / 100d;

        double total = subtotal - itemsDiscount - invoiceDiscount + vat;
        double paid = ParseAmount(PaidEntry.Text);
        double remaining = total - paid;

        SubtotalLabel.Text = subtotal.ToString("0.00", CultureInfo.InvariantCulture);
        ItemsDiscountLabel.Text = itemsDiscount.ToString("0.00", CultureInfo.InvariantCulture);
        InvoiceDiscountLabel.Text = invoiceDiscount.ToString("0.00", CultureInfo.InvariantCulture);
        VatLabel.Text = vat.ToString("0.00", CultureInfo.InvariantCulture);
        GrandTotalLabel.Text = total.ToString("0.00", CultureInfo.InvariantCulture);
        RemainingLabel.Text = remaining.ToString("0.00", CultureInfo.InvariantCulture);
    }

    // ---------------------------------------------------------------------
    // الحفظ
    // ---------------------------------------------------------------------

    private async void OnSaveClicked(object? sender, EventArgs e) => await SaveAsync(close: false);

    private async void OnSaveAndCloseClicked(object? sender, EventArgs e) => await SaveAsync(close: true);

    private async Task SaveAsync(bool close)
    {
        if (_saving || _invoice is null)
        {
            return;
        }

        if (_rows.Count == 0)
        {
            bool confirmed = await DisplayAlertAsync(
                "لا توجد بنود",
                "هذه الفاتورة بدون أي صنف. هل تريد حفظها على هذا الأساس؟",
                "حفظ", "إلغاء");

            if (!confirmed)
            {
                return;
            }
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        SaveButton.Text = "جارٍ الحفظ...";

        try
        {
            var invoice = _invoice;
            var items = new List<InvoiceItem>();

            double subtotal = 0;
            double itemsDiscount = 0;

            foreach (ItemRow row in _rows)
            {
                double quantity = ParseAmount(row.QuantityEntry.Text);
                double price = ParseAmount(row.PriceEntry.Text);
                double percent = Math.Clamp(ParseAmount(row.DiscountPercentEntry.Text), 0, 100);

                double gross = quantity * price;
                double discount = gross * percent / 100d;

                subtotal += gross;
                itemsDiscount += discount;

                items.Add(new InvoiceItem
                {
                    Id = row.Item.Id,
                    InvoiceId = invoice.Id,
                    Barcode = row.Item.Barcode,
                    Name = row.Item.Name,
                    Unit = row.Item.Unit,
                    Quantity = quantity,
                    Price = price,
                    DiscountPercent = percent,
                    DiscountAmount = discount,
                    Total = gross - discount,
                    Note = row.Item.Note
                });
            }

            double invoiceDiscountPercent = Math.Clamp(ParseAmount(InvoiceDiscountEntry.Text), 0, 100);
            double invoiceDiscount = (subtotal - itemsDiscount) * invoiceDiscountPercent / 100d;

            double vatPercent = Math.Clamp(ParseAmount(VatPercentEntry.Text), 0, 100);
            double vat = (subtotal - itemsDiscount - invoiceDiscount) * vatPercent / 100d;

            double total = subtotal - itemsDiscount - invoiceDiscount + vat;
            double paid = ParseAmount(PaidEntry.Text);

            invoice.Customer = string.IsNullOrWhiteSpace(CustomerEntry.Text)
                ? invoice.Customer
                : CustomerEntry.Text.Trim();
            // ملاحظة: في .NET MAUI 10 أصبحت DatePicker.Date من نوع DateTime? (nullable)
            DateTime pickedDate = InvoiceDatePicker.Date ?? DateTime.Now;
            invoice.InvoiceDate = pickedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            invoice.InvoiceType = InvoiceTypePicker.SelectedItem as string ?? invoice.InvoiceType;
            invoice.PriceList = PriceListPicker.SelectedItem as string ?? invoice.PriceList;

            invoice.Subtotal = subtotal;
            invoice.ItemDiscount = itemsDiscount;
            invoice.InvoiceDiscount = invoiceDiscount;
            invoice.Vat = vat;
            invoice.Total = total;
            invoice.Paid = paid;
            invoice.Remaining = total - paid;

            Invoice? saved = await InvoiceSource.UpdateAsync(invoice, items);

            if (saved is not null)
            {
                _invoice = saved;
            }

            ShowStatus("تم حفظ التعديلات بنجاح ✓", "#25A36F");
            SubtitleLabel.Text = invoice.Customer;

            if (close)
            {
                await Task.Delay(450);
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"تعذّر الحفظ: {ex.Message}", "#D9534F");
        }
        finally
        {
            _saving = false;
            SaveButton.IsEnabled = true;
            SaveButton.Text = "💾 حفظ التعديلات";
        }
    }

    private async void ShowStatus(string message, string color)
    {
        StatusText.Text = message;
        StatusBanner.BackgroundColor = Color.FromArgb(color);
        StatusBanner.IsVisible = true;

        await Task.Delay(4000);
        StatusBanner.IsVisible = false;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
