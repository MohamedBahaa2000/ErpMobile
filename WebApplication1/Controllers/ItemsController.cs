using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل الأصناف (GET: api/items)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            return await _context.Items.ToListAsync();
        }

        // 2. جلب صنف واحد بالـ ID (GET: api/items/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound(new { message = "الصنف غير موجود!" });
            }

            return item;
        }

        // 3. إضافة صنف جديد (POST: api/items)
        [HttpPost]
        public async Task<ActionResult<Item>> PostItem([FromBody] Item item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }

        // 4. تعديل صنف موجود (PUT: api/items/5)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, [FromBody] Item item)
        {
            if (id != item.Id)
            {
                return BadRequest(new { message = "رقم الصنف غير متطابق!" });
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(id))
                {
                    return NotFound(new { message = "الصنف غير موجود!" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "تم تعديل الصنف بنجاح", item });
        }

        // 5. حذف صنف (DELETE: api/items/5)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound(new { message = "الصنف غير موجود!" });
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف الصنف بنجاح" });
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }
    }
}