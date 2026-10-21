/* ═══════════════════════════════════════════════════════════════════
   إصلاح تسجيل المستخدمين + إرجاع حساب admin
   ───────────────────────────────────────────────────────────────────
   المشكلة دلوقتي:
     • قاعدة البيانات اتعملها RESET ⇒ جدول Users فاضي
     • /api/Auth/register بيرجّع 500 (عمود Email NOT NULL)
     ⇒ محدش يقدر يسجّل دخول (admin بقى 401)

   ► شغّل هذا الملف في SSMS على 192.168.100.70
   ═══════════════════════════════════════════════════════════════════ */

USE ERP_Db;
GO

/* ── 1) عرض شكل جدول Users الحالي ─────────────────────────────── */
PRINT '=== أعمدة dbo.Users ===';
SELECT
    c.name          AS ColumnName,
    t.name          AS DataType,
    c.max_length    AS [Length],
    c.is_nullable   AS Nullable,
    COLUMN_DEFAULT  AS [Default]
FROM sys.columns c
JOIN sys.types  t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Users')
ORDER BY c.column_id;
GO

/* ── 2) جعل Email اختيارياً (يلغي خطأ التسجيل) ─────────────────── */
IF COL_LENGTH(N'dbo.Users', N'Email') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Users ALTER COLUMN Email NVARCHAR(256) NULL;
    PRINT '✅ تم جعل dbo.Users.Email اختيارياً (يقبل NULL).';
END
ELSE
    PRINT '⚠️ عمود Email غير موجود — راجع الجدول أعلاه.';
GO

/* ── 3) إنشاء/تحديث حساب admin بكلمة المرور 123456 ─────────────
        الباسورد مشفّر بصيغة Microsoft.AspNetCore.Identity
        (PasswordHasher v2 — PBKDF2-HMAC-SHA256، 10000 تكرار،
         salt 12 بايت، subkey 32 بايت)
        VerifyHashedPassword بيرجّع true للباسورد "123456"   */
DECLARE @Hash NVARCHAR(200) =
    N'ARAnAABwOqbmG5Vm6rUCJQue1H4+43Iu+6ritGM5fks/qu/qPzCz4AriMSUN8wIXww==';

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    UPDATE dbo.Users
    SET Password = @Hash,
        Email    = COALESCE(NULLIF(Email, ''), 'admin@local')
    WHERE Username = 'admin';
    PRINT '✅ تم تحديث حساب admin (كلمة المرور: 123456)';
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Username, Password, Email)
    VALUES (N'admin', @Hash, N'admin@local');
    PRINT '✅ تم إنشاء حساب admin (كلمة المرور: 123456)';
END
GO

/* ── 4) تأكيد ──────────────────────────────────────────────────── */
PRINT '=== المستخدمون الحاليون ===';
SELECT Id, Username, Email FROM dbo.Users ORDER BY Id;
GO
