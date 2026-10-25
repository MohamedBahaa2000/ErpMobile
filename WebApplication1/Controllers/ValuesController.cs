using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // Endpoint لجلب قائمة الأصناف لزمايلك في الـ UI
        [HttpGet]
        public IActionResult GetValues()
        {
            var items = new List<object>
            {
                new { Id = 1, Name = "صنف 1 - لابتوب ديل", Unit = "قطعة", Price = 35000 },
                new { Id = 2, Name = "صنف 2 - طابعة إبسون", Unit = "قطعة", Price = 12000 },
                new { Id = 3, Name = "صنف 3 - باراركود ريدر", Unit = "قطعة", Price = 2500 }
            };

            return Ok(items);
        }
    }
}