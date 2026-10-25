/* ═══════════════════════════════════════════════════════════════════
   كونترولرات المبيعات والمشتريات — ملف واحد
   الملف: WebApplication1\Controllers\InvoicesController.cs
   ───────────────────────────────────────────────────────────────────
   المسارات اللي التطبيق (MAUI) بيستخدمها:

   المبيعات:
     GET    /api/Invoices              كل الفواتير (مع البنود)
     GET    /api/Invoices/{id}         فاتورة واحدة
     POST   /api/Invoices              إنشاء (مع البنود)
     PUT    /api/Invoices/{id}         تعديل
     DELETE /api/Invoices/{id}         حذف (يحذف البنود معها)

   المشتريات:
     GET    /api/PurchaseInvoices      كل الفواتير (مع البنود)
     GET    /api/PurchaseInvoices/{id} فاتورة واحدة
     POST   /api/PurchaseInvoices      إنشاء (مع البنود)
     PUT    /api/PurchaseInvoices/{id} تعديل
     DELETE /api/PurchaseInvoices/{id} حذف (يحذف البنود معها)

   ⚠️ لا تضف خاصية «Invoice» داخل InvoiceItem
      ولا «PurchaseInvoice» داخل PurchaseInvoiceItem
      — دي سبب 500 Internal Server Error (حلقة دائرية في JSON)
   ═══════════════════════════════════════════════════════════════════ */

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    // ╔═══════════════════════════════════════════════════════════╗
    // ║  1) فواتير المبيعات  →  /api/Invoices                    ║
    // ╚═══════════════════════════════════════════════════════════╝
    [Route("api/Invoices")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // ── 1) كل فواتير المبيعات ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var invoices = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.InvoiceItems)
                    .OrderByDescending(i => i.Id)
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب فواتير المبيعات", error = ex.Message });
            }
        }

        // ── 2) فاتورة واحدة بالمعرّف ────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الفاتورة", error = ex.Message });
            }
        }

        // ── 3) فاتورة برقمها ────────────────────────────────────────
        [HttpGet("number/{number:int}")]
        public async Task<IActionResult> GetByNumber(int number)
        {
            try
            {
                var invoice = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.InvoiceNumber == number);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الفاتورة", error = ex.Message });
            }
        }

        // ── 4) إنشاء فاتورة مبيعات (مع بنودها) ─────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // منع تكرار رقم الفاتورة
                bool exists = await _context.Invoices
                    .AnyAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);

                if (exists)
                    return BadRequest(new { message = $"رقم الفاتورة {invoice.InvoiceNumber} مستخدم من قبل!" });

                if (string.IsNullOrWhiteSpace(invoice.InvoiceDate))
                    invoice.InvoiceDate = DateTime.Now.ToString("yyyy-MM-dd");

                invoice.Id = 0; // الهوية التلقائية

                if (invoice.InvoiceItems is not null)
                {
                    foreach (var item in invoice.InvoiceItems)
                    {
                        item.Id = 0;          // الهوية التلقائية
                        item.InvoiceId = 0;   // هيتضبط تلقائياً بعد الحفظ
                    }
                }

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync(); // هنا بيتحدد Id ويتربط البنود

                // إعادة الجلب بالبنود لتأكيد تطابق الإسناد
                var saved = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.InvoiceItems)
                    .FirstAsync(i => i.Id == invoice.Id);

                return Ok(new { message = "تم حفظ فاتورة المبيعات بنجاح", invoiceId = invoice.Id, invoice = saved });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء حفظ الفاتورة", error = ex.Message });
            }
        }

        // ── 5) تعديل فاتورة مبيعات ──────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Invoice input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                invoice.InvoiceNumber = input.InvoiceNumber;
                invoice.InvoiceDate = input.InvoiceDate;
                invoice.Store = input.Store;
                invoice.InvoiceType = input.InvoiceType;
                invoice.Customer = input.Customer;
                invoice.SalesRep = input.SalesRep;
                invoice.Driver = input.Driver;
                invoice.PriceList = input.PriceList;
                invoice.ApproveJournal = input.ApproveJournal;
                invoice.ApproveStoreMove = input.ApproveStoreMove;
                invoice.Subtotal = input.Subtotal;
                invoice.ItemDiscount = input.ItemDiscount;
                invoice.InvoiceDiscount = input.InvoiceDiscount;
                invoice.Vat = input.Vat;
                invoice.Total = input.Total;
                invoice.Paid = input.Paid;
                invoice.Remaining = input.Remaining;
                invoice.DiscountReason = input.DiscountReason;

                // استبدال البنود بالكامل
                if (invoice.InvoiceItems is not null && invoice.InvoiceItems.Count > 0)
                    _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);

                if (input.InvoiceItems is not null)
                {
                    foreach (var item in input.InvoiceItems)
                    {
                        item.Id = 0;
                        item.InvoiceId = id;
                        _context.InvoiceItems.Add(item);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تعديل فاتورة المبيعات بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء تعديل الفاتورة", error = ex.Message });
            }
        }

        // ── 6) حذف فاتورة مبيعات (يحذف البنود معها) ────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                if (invoice.InvoiceItems is not null && invoice.InvoiceItems.Count > 0)
                    _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف فاتورة المبيعات بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف الفاتورة", error = ex.Message });
            }
        }
    }

    // ╔═══════════════════════════════════════════════════════════╗
    // ║  2) فواتير المشتريات  →  /api/PurchaseInvoices            ║
    // ╚═══════════════════════════════════════════════════════════╝
    [Route("api/PurchaseInvoices")]
    [ApiController]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseInvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // ── 1) كل فواتير المشتريات ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var invoices = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Include(i => i.PurchaseInvoiceItems)
                    .OrderByDescending(i => i.Id)
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب فواتير المشتريات", error = ex.Message });
            }
        }

        // ── 2) فاتورة واحدة بالمعرّف ────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var invoice = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الفاتورة", error = ex.Message });
            }
        }

        // ── 3) إنشاء فاتورة مشتريات (مع بنودها) ────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseInvoice invoice)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                bool exists = await _context.PurchaseInvoices
                    .AnyAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);

                if (exists)
                    return BadRequest(new { message = $"رقم الفاتورة {invoice.InvoiceNumber} مستخدم من قبل!" });

                if (string.IsNullOrWhiteSpace(invoice.InvoiceDate))
                    invoice.InvoiceDate = DateTime.Now.ToString("yyyy-MM-dd");

                invoice.Id = 0;

                if (invoice.PurchaseInvoiceItems is not null)
                {
                    foreach (var item in invoice.PurchaseInvoiceItems)
                    {
                        item.Id = 0;
                        item.InvoiceId = 0; // هيتضبط تلقائياً بعد الحفظ
                    }
                }

                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                var saved = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstAsync(i => i.Id == invoice.Id);

                return Ok(new { message = "تم حفظ فاتورة المشتريات بنجاح", invoiceId = invoice.Id, invoice = saved });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء حفظ فاتورة المشتريات", error = ex.Message });
            }
        }

        // ── 4) تعديل فاتورة مشتريات ────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PurchaseInvoice input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var invoice = await _context.PurchaseInvoices
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                invoice.InvoiceNumber = input.InvoiceNumber;
                invoice.InvoiceDate = input.InvoiceDate;
                invoice.Store = input.Store;
                invoice.InvoiceType = input.InvoiceType;
                invoice.Supplier = input.Supplier;
                invoice.Rep = input.Rep;
                invoice.Driver = input.Driver;
                invoice.PriceList = input.PriceList;
                invoice.ApproveJournal = input.ApproveJournal;
                invoice.ApproveStoreMove = input.ApproveStoreMove;
                invoice.Subtotal = input.Subtotal;
                invoice.ItemDiscount = input.ItemDiscount;
                invoice.InvoiceDiscount = input.InvoiceDiscount;
                invoice.Vat = input.Vat;
                invoice.Total = input.Total;
                invoice.Paid = input.Paid;
                invoice.Remaining = input.Remaining;
                invoice.DiscountReason = input.DiscountReason;

                if (invoice.PurchaseInvoiceItems is not null && invoice.PurchaseInvoiceItems.Count > 0)
                    _context.PurchaseInvoiceItems.RemoveRange(invoice.PurchaseInvoiceItems);

                if (input.PurchaseInvoiceItems is not null)
                {
                    foreach (var item in input.PurchaseInvoiceItems)
                    {
                        item.Id = 0;
                        item.InvoiceId = id;
                        _context.PurchaseInvoiceItems.Add(item);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تعديل فاتورة المشتريات بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء تعديل فاتورة المشتريات", error = ex.Message });
            }
        }

        // ── 5) حذف فاتورة مشتريات (يحذف البنود معها) ───────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var invoice = await _context.PurchaseInvoices
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice is null)
                    return NotFound(new { message = "لم يتم العثور على الفاتورة" });

                if (invoice.PurchaseInvoiceItems is not null && invoice.PurchaseInvoiceItems.Count > 0)
                    _context.PurchaseInvoiceItems.RemoveRange(invoice.PurchaseInvoiceItems);

                _context.PurchaseInvoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف فاتورة المشتريات بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف فاتورة المشتريات", error = ex.Message });
            }
        }
    }
}