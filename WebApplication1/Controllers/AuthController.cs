using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // 1. تسجيل مستخدم جديد (Register) باستخدام Username و Password فقط
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // التحقق أن كلمة المرور لا تقل عن 6 أحرف
            if (string.IsNullOrEmpty(user.Password) || user.Password.Length < 6)
            {
                return BadRequest(new { message = "كلمة المرور يجب ألا تقل عن 6 أحرف أو أرقام" });
            }

            // التأكد إن اسم المستخدم (Username) مش مسجل قبل كده
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (existingUser != null)
            {
                return BadRequest(new { message = "اسم المستخدم مستخدم من قبل!" });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم التسجيل بنجاح", userId = user.Id });
        }

        // 2. تسجيل الدخول (Login) باستخدام Username والمقارنة المباشرة البسيطة
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Username) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest(new { message = "يرجى إدخال اسم المستخدم وكلمة المرور" });
            }

            // البحث عن المستخدم بـ Username فقط
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
            if (user == null || user.Password != loginDto.Password)
            {
                return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة!" });
            }

            return Ok(new { message = "تم تسجيل الدخول بنجاح", username = user.Username });
        }
    }
}