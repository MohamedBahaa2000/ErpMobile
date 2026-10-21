# 🔌 تركيب المبيعات والمشتريات على الـ API (جهاز 192.168.100.70)

## الخطوات بالترتيب

### 1️⃣ إنشاء الجداول في قاعدة البيانات

افتح **SSMS** على `.70` وشغّل:

```
tools\create_sales_purchase_tables.sql
```

بيإنشاء 4 جداول (آمن للتكرار):

| الجدول | المحتوى |
|---|---|
| `dbo.Invoices` | فواتير المبيعات — 19 عمود |
| `dbo.InvoiceItems` | بنود المبيعات — 11 عمود |
| `dbo.PurchaseInvoices` | فواتير المشتريات — 19 عمود |
| `dbo.PurchaseInvoiceItems` | بنود المشتريات — 11 عمود |

> في الآخر بيطلع **تقرير** بعدد السجلات والأعمدة للتأكيد.

---

### 2️⃣ إضافة الموديلات

انسخ محتوى `tools\api-controllers\InvoiceModels.cs` إلى:

```
F:\WebApplication1\WebApplication1\Models\InvoiceModels.cs
```

(أو الصق الكلاسات الأربعة في ملف موجود там)

---

### 3️⃣ تسجيل الجداول في `AppDbContext`

افتح `F:\WebApplication1\WebApplication1\Data\AppDbContext.cs` وأضف **4 صفوف**:

```csharp
public DbSet<Invoice>              Invoices             { get; set; }
public DbSet<InvoiceItem>          InvoiceItems         { get; set; }
public DbSet<PurchaseInvoice>      PurchaseInvoices     { get; set; }
public DbSet<PurchaseInvoiceItem>  PurchaseInvoiceItems { get; set; }
```

وإن كنت بتستخدم Fluent API، أضف في `OnModelCreating` (اختياري):

```csharp
modelBuilder.Entity<InvoiceItem>()
    .HasOne<Invoice>().WithMany(i => i.InvoiceItems)
    .HasForeignKey(i => i.InvoiceId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<PurchaseInvoiceItem>()
    .HasOne<PurchaseInvoice>().WithMany(i => i.PurchaseInvoiceItems)
    .HasForeignKey(i => i.InvoiceId)
    .OnDelete(DeleteBehavior.Cascade);
```

---

### 4️⃣ إضافة الكونترولرات

انسخ الملفين إلى:

```
F:\WebApplication1\WebApplication1\Controllers\InvoicesController.cs
F:\WebApplication1\WebApplication1\Controllers\PurchaseInvoicesController.cs
```

---

### 5️⃣ تشغيل واختبار

```powershell
dotnet build
dotnet run
```

ثم تحقق:

```powershell
$base = 'http://192.168.100.70:5034'
Invoke-RestMethod "$base/api/Invoices"           # المفروض []
Invoke-RestMethod "$base/api/PurchaseInvoices"   # المفروض []
```

---

## ⚠️ تحذير مهم — انتبه له

**لا تضف خاصية إسناد للبنود** (لا `Invoice` داخل `InvoiceItem` ولا `PurchaseInvoice` داخل `PurchaseInvoiceItem`).

لو ضفتها بيحصل:

```
Invoice ← InvoiceItems ← Invoice ← InvoiceItems ← ...  ♾️
```

وده بيسبب **`500 Internal Server Error`** — وده بالظبط اللي حصل قبل كده مع `GET /api/Invoices`.

الربط بيتم **بالعمود `InvoiceId` العادي** بس، والكونترولر بيربطه يدوياً.

---

## 📡 شكل البيانات المتوقع من التطبيق

### `POST /api/Invoices`
```json
{
  "invoiceNumber": 1001,
  "invoiceDate": "2026-10-20",
  "customer": "عميل نقدي",
  "invoiceType": "نقدي",
  "store": "Main Store",
  "total": 200, "paid": 200, "remaining": 0,
  "invoiceItems": [
    { "barcode": "6224007860670", "name": "علبة مناديل",
      "unit": "قطعة", "quantity": 2, "price": 100, "total": 200 }
  ]
}
```

### `POST /api/PurchaseInvoices`
نفس الشكل بس بـ `supplier` و `rep` بدل `customer` و `salesRep`.

---

## 🔁 لو حبيت تعمل نسخة احتياطية

```powershell
# على .70
sqlcmd -S localhost -E -Q "BACKUP DATABASE ERP_Db TO DISK='C:\backup\erp.bak' WITH INIT"
```
