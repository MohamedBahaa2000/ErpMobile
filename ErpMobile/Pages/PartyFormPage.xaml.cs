using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

[QueryProperty(nameof(PartyId), "id")]
[QueryProperty(nameof(PartyType), "type")]
public partial class PartyFormPage : ContentPage
{
    private Party? _existing;

    public int PartyId { get; set; }
    public string PartyType { get; set; } = Party.Customer;

    public PartyFormPage()
    {
        InitializeComponent();
        PaymentKindPicker.ItemsSource = new[] { "نقدي", "آجل" };
        PaymentKindPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool suppliers = PartyType == Party.Supplier;

        if (_existing is null && PartyId > 0)
        {
            // المعرّف هنا هو معرّف الخادم (لأن القائمة تأتي من الـ API)
            _existing = await PartySource.GetByIdAsync(PartyId);
        }

        FormTitleLabel.Text = _existing is null
            ? (suppliers ? "إضافة مورد" : "إضافة عميل")
            : (suppliers ? "تعديل مورد" : "تعديل عميل");

        if (_existing is not null)
        {
            NameEntry.Text = _existing.Name;
            EnglishNameEntry.Text = _existing.EnglishName;
            CodeEntry.Text = _existing.Code;
            MobileEntry.Text = _existing.Mobile;
            PhoneEntry.Text = _existing.Phone;
            TaxNumberEntry.Text = _existing.TaxNumber;
            CommercialRegEntry.Text = _existing.CommercialReg;
            EmailEntry.Text = _existing.Email;
            AddressEditor.Text = _existing.Address;
            NotesEditor.Text = _existing.Notes;
            OpeningBalanceEntry.Text = _existing.OpeningBalance.ToString("0.00", CultureInfo.InvariantCulture);

            int kindIndex = -1;
            for (int index = 0; index < PaymentKindPicker.Items.Count; index++)
            {
                if (PaymentKindPicker.Items[index] == _existing.PaymentKind)
                {
                    kindIndex = index;
                    break;
                }
            }

            PaymentKindPicker.SelectedIndex = kindIndex >= 0 ? kindIndex : 0;
        }
    }

    private async void OnSaveAndNewClicked(object? sender, EventArgs e)
    {
        if (await SaveAsync())
        {
            _existing = null;
            PartyId = 0;
            ClearForm();
            ToastService.Success("تم حفظ السجل ✓ أدخل السجل التالي");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (await SaveAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task<bool> SaveAsync()
    {
        string name = NameEntry.Text?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            await DisplayAlertAsync("الاسم مطلوب", "أدخل اسم السجل أولاً.", "موافق");
            return false;
        }

        _ = double.TryParse(OpeningBalanceEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double openingBalance);

        if (_existing is null)
        {
            var party = new Party
            {
                Type = PartyType,
                Name = name,
                EnglishName = EnglishNameEntry.Text?.Trim() ?? string.Empty,
                Mobile = MobileEntry.Text?.Trim() ?? string.Empty,
                Phone = PhoneEntry.Text?.Trim() ?? string.Empty,
                TaxNumber = TaxNumberEntry.Text?.Trim() ?? string.Empty,
                CommercialReg = CommercialRegEntry.Text?.Trim() ?? string.Empty,
                Email = EmailEntry.Text?.Trim() ?? string.Empty,
                Address = AddressEditor.Text?.Trim() ?? string.Empty,
                Notes = NotesEditor.Text?.Trim() ?? string.Empty,
                PaymentKind = PaymentKindPicker.SelectedItem?.ToString() ?? "نقدي",
                OpeningBalance = openingBalance
            };

            // إضافة: على الخادم أولاً ثم نسخها للجهاز (نفس مصدر قائمة العملاء)
            Party? saved = await PartySource.CreateAsync(party);
            if (saved is null)
            {
                await DisplayAlertAsync("الاسم مسجل", "يوجد سجل بنفس الاسم بالفعل، أو تعذّر الاتصال بالخادم.", "موافق");
                return false;
            }

            _existing = saved;
            PartyId = saved.Id;
        }
        else
        {
            _existing.Name = name;
            _existing.EnglishName = EnglishNameEntry.Text?.Trim() ?? string.Empty;
            _existing.Mobile = MobileEntry.Text?.Trim() ?? string.Empty;
            _existing.Phone = PhoneEntry.Text?.Trim() ?? string.Empty;
            _existing.TaxNumber = TaxNumberEntry.Text?.Trim() ?? string.Empty;
            _existing.CommercialReg = CommercialRegEntry.Text?.Trim() ?? string.Empty;
            _existing.Email = EmailEntry.Text?.Trim() ?? string.Empty;
            _existing.Address = AddressEditor.Text?.Trim() ?? string.Empty;
            _existing.Notes = NotesEditor.Text?.Trim() ?? string.Empty;
            _existing.PaymentKind = PaymentKindPicker.SelectedItem?.ToString() ?? "نقدي";
            _existing.OpeningBalance = openingBalance;

            await PartySource.UpdateAsync(_existing);
        }

        return true;
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        EnglishNameEntry.Text = string.Empty;
        CodeEntry.Text = string.Empty;
        MobileEntry.Text = string.Empty;
        PhoneEntry.Text = string.Empty;
        TaxNumberEntry.Text = string.Empty;
        CommercialRegEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        AddressEditor.Text = string.Empty;
        NotesEditor.Text = string.Empty;
        OpeningBalanceEntry.Text = "0.00";
        PaymentKindPicker.SelectedIndex = 0;
        NameEntry.Focus();
    }
}
