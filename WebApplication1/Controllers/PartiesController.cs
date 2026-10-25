using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartiesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل العملاء والموردين (GET: api/parties)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Party>>> GetParties()
        {
            return await _context.Parties.ToListAsync();
        }

        // 2. جلب عميل واحد بالـ ID (GET: api/parties/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<Party>> GetParty(int id)
        {
            var party = await _context.Parties.FindAsync(id);

            if (party == null)
            {
                return NotFound(new { message = "العميل أو المورد غير موجود!" });
            }

            return party;
        }

        // 3. إضافة عميل أو مورد جديد (POST: api/parties)
        [HttpPost]
        public async Task<ActionResult<Party>> PostParty([FromBody] Party party)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParty), new { id = party.Id }, party);
        }

        // 4. تعديل بيانات عميل أو مورد (PUT: api/parties/5)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutParty(int id, [FromBody] Party party)
        {
            if (id != party.Id)
            {
                return BadRequest(new { message = "رقم العميل غير متطابق!" });
            }

            _context.Entry(party).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartyExists(id))
                {
                    return NotFound(new { message = "العميل غير موجود!" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "تم تعديل البيانات بنجاح", party });
        }

        // 5. حذف عميل أو مورد (DELETE: api/parties/5)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParty(int id)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
            {
                return NotFound(new { message = "العميل غير موجود!" });
            }

            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف العميل بنجاح" });
        }

        private bool PartyExists(int id)
        {
            return _context.Parties.Any(e => e.Id == id);
        }
    }
}