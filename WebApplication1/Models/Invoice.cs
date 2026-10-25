/* ═══════════════════════════════════════════════════════════════════
   موديلات المبيعات والمشتريات — WebApplication1\Models
   ───────────────────────────────────────────────────────────────────
   ⚠️ الأسماء والأنواع هنا لازم **تطابق تطبيق MAUI بالحرف**،
      غير كده الربط بينهم هيتكسر.

   أهم 3 قواعد:
   1) InvoiceNumber   = int      (مش string — التطبيق بيبعت رقم)
   2) InvoiceDate     = string   (مش DateTime — التطبيق بيبعت "2026-10-20"
                                 وبيقارنها نصّاً لحساب "فواتير اليوم")
   3) لا توجد خاصية Invoice داخل InvoiceItem
                                 (حلقة دائرية في JSON ⇒ 500 Error)
   ═══════════════════════════════════════════════════════════════════ */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    // ═══════════════════ 1) فاتورة مبيعات ═══════════════════

    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        /// <summary>رقم الفاتورة — يبدأ من 1001 في التطبيق.</summary>
        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        public int InvoiceNumber { get; set; }

        /// <summary>التاريخ بصيغة yyyy-MM-dd (نص وليس DateTime).</summary>
        [Required(ErrorMessage = "تاريخ الفاتورة مطلوب")]
        public string InvoiceDate { get; set; } = string.Empty;

        public string Store { get; set; } = "Main Store";

        /// <summary>نقدي / آجل.</summary>
        public string InvoiceType { get; set; } = "نقدي";

        /// <summary>اسم العميل — كان اسمه CustomerName في النسخة القديمة.</summary>
        [Required(ErrorMessage = "اسم العميل مطلوب")]
        public string Customer { get; set; } = "عميل نقدي";

        public string SalesRep { get; set; } = "-- اختر --";

        public string Driver { get; set; } = "-- اختر --";

        public string PriceList { get; set; } = "-- اختر --";

        public string ApproveJournal { get; set; } = "معتمد";

        public string ApproveStoreMove { get; set; } = "معتمد";

        public decimal Subtotal { get; set; }

        public decimal ItemDiscount { get; set; }

        public decimal InvoiceDiscount { get; set; }

        public decimal Vat { get; set; }

        /// <summary>الإجمالي — كان اسمه TotalAmount في النسخة القديمة.</summary>
        [Required(ErrorMessage = "قيمة الفاتورة مطلوبة")]
        public decimal Total { get; set; }

        public decimal Paid { get; set; }

        /// <summary>المتبقي — بديل حقل Status القديم (Pending/Paid).</summary>
        public decimal Remaining { get; set; }

        public string? DiscountReason { get; set; }

        /// <summary>بنود الفاتورة — تُحمَّل عند القراءة وتُحفَظ عند الإنشاء.</summary>
        public List<InvoiceItem> InvoiceItems { get; set; } = new();
    }

    // ═══════════════════ 2) بند فاتورة مبيعات ═══════════════════

    [Table("InvoiceItems")]
    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public string? Barcode { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Unit { get; set; } = "قطعة";

        public decimal Quantity { get; set; } = 1;

        public decimal Price { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal Total { get; set; }

        public string? Note { get; set; }

        // ⛔ لا تضف هنا خاصية Invoice — سبب 500 Internal Server Error
    }

    // ═══════════════════ 3) فاتورة مشتريات ═══════════════════

    [Table("PurchaseInvoices")]
    public class PurchaseInvoice
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        public int InvoiceNumber { get; set; }

        /// <summary>yyyy-MM-dd (نص وليس DateTime).</summary>
        [Required(ErrorMessage = "تاريخ الفاتورة مطلوب")]
        public string InvoiceDate { get; set; } = string.Empty;

        public string Store { get; set; } = "Main Store";

        public string InvoiceType { get; set; } = "نقدي";

        /// <summary>اسم المورد (بديل Customer في المبيعات).</summary>
        [Required(ErrorMessage = "اسم المورد مطلوب")]
        public string Supplier { get; set; } = "مورد نقدي";

        /// <summary>المندوب (بديل SalesRep في المبيعات).</summary>
        public string Rep { get; set; } = "-- اختر --";

        public string Driver { get; set; } = "-- اختر --";

        public string PriceList { get; set; } = "-- اختر --";

        public string ApproveJournal { get; set; } = "معتمد";

        public string ApproveStoreMove { get; set; } = "معتمد";

        public decimal Subtotal { get; set; }

        public decimal ItemDiscount { get; set; }

        public decimal InvoiceDiscount { get; set; }

        public decimal Vat { get; set; }

        public decimal Total { get; set; }

        public decimal Paid { get; set; }

        public decimal Remaining { get; set; }

        public string? DiscountReason { get; set; }

        public List<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new();
    }

    // ═══════════════════ 4) بند فاتورة مشتريات ═══════════════════

    [Table("PurchaseInvoiceItems")]
    public class PurchaseInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public string? Barcode { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Unit { get; set; } = "قطعة";

        public decimal Quantity { get; set; } = 1;

        public decimal Price { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal Total { get; set; }

        public string? Note { get; set; }

        // ⛔ لا تضف هنا خاصية PurchaseInvoice — سبب 500 Internal Server Error
    }
}