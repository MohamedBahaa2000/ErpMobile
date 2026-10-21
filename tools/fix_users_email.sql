/* ═══════════════════════════════════════════════════════════════════
   إصلاح تسجيل المستخدمين — /api/Auth/register بيرجّع 500
   ───────────────────────────────────────────────────────────────────
   السبب:
     AuthController.Register(User user) بيحاول يحفظ يوزر جديد
     بس عمود Email في جدول Users هو NOT NULL، والتسجيل مش بيبعته
     ⇒ INSERT بيفشل بـ: "Cannot insert the value NULL into column 'Email'"

   الحل: جعل Email اختياري (NULL مسموح).

   ► شغّل هذا الملف في SSMS على جهاز 192.168.100.70
   ═══════════════════════════════════════════════════════════════════ */

USE ERP_Db;
GO

-- ── 1) عرض الوضع الحالي ──────────────────────────────────────────
SELECT
    c.name  AS ColumnName,
    t.name  AS DataType,
    c.max_length,
    c.is_nullable
FROM sys.columns c
JOIN sys.types  t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Users')
ORDER BY c.column_id;
GO

-- ── 2) جعل Email اختيارياً ───────────────────────────────────────
IF COL_LENGTH(N'dbo.Users', N'Email') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN Email NVARCHAR(256) NULL;
    PRINT '✅ تم جعل dbo.Users.Email اختيارياً (يقبل NULL).';
END
ELSE
BEGIN
    PRINT '⚠️ عمود Email غير موجود في dbo.Users — راجع النتيجة أعلاه لأسماء الأعمدة.';
END
GO

-- ── 3) (اختياري) إعطاء قيمة افتراضية للسجلات القديمة الفارغة ────
UPDATE dbo.Users
SET Email = CONCAT(Username, '@local')
WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = '';
GO

-- ── 4) تأكيد ─────────────────────────────────────────────────────
SELECT c.name AS ColumnName, c.is_nullable
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Users')
ORDER BY c.column_id;
GO
