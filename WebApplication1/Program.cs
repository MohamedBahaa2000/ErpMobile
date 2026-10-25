using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// إعداد الاتصال بقاعدة البيانات SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// إنشاء قاعدة البيانات + تشغيل كل الـ Migrations تلقائياً عند الإقلاع.
// كده المشروع بيشتغل على أي جهاز جديد من غير أي خطوة يدوية.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ⚠️ ملاحظة: ممنوع UseHttpsRedirection هنا — التطبيق متصل بالموبايل عبر http:// صريح.
app.UseAuthorization();

app.MapControllers();

app.Run();