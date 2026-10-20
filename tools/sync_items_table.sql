/* ============================================================================
   ErpMobile — محاذاة جدول الأصناف  (Items)
   قاعدة بيانات: SQL Server (T-SQL)

   الغرض: جعل جدول الأصناف في قاعدة بيانات الـ API مطابقاً لنموذج Product
          في تطبيق ErpMobile (17 حقل).

   خصائص السكربت:
     • آمن وقابل للتكرار (Idempotent) — تشغّله كذا مرة بدون مشكلة
     • لو الجدول مش موجود → ينشئه بالكامل
     • لو الجدول موجود   → يضيف الأعمدة الناقصة فقط (لا يمسح ولا يعدّل بياناتك)
     • النصوص NVARCHAR  → يدعم العربي بدون تقطيع
     • الفلوس DECIMAL(18,2) → دقة محاسبية صحيحة
   ============================================================================ */

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @tbl NVARCHAR(128) = N'Items';   -- غيّره إذا كان اسم جدولك مختلف (مثلاً: Products)

/* ---------------------------------------------------------------------------
   1) إنشاء الجدول إذا لم يكن موجوداً
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.' + @tbl, N'U') IS NULL
BEGIN
    EXEC(N'
        CREATE TABLE dbo.' + @tbl + N'
        (
            Id             INT IDENTITY(1,1)  NOT NULL PRIMARY KEY,
            Barcode        NVARCHAR(100)      NULL,
            Name           NVARCHAR(200)      NOT NULL,
            Price          DECIMAL(18,2)      NOT NULL CONSTRAINT DF_' + @tbl + N'_Price DEFAULT (0),
            Unit           NVARCHAR(50)       NOT NULL CONSTRAINT DF_' + @tbl + N'_Unit DEFAULT (N''قطعة''),
            EnglishName    NVARCHAR(200)      NULL,
            HelperCode     NVARCHAR(100)      NULL,
            Origin         NVARCHAR(50)       NULL,
            Category       NVARCHAR(100)      NULL,
            Manufacturer   NVARCHAR(150)      NULL,
            CompanyCode    NVARCHAR(100)      NULL,
            CostMethod     NVARCHAR(50)       NOT NULL CONSTRAINT DF_' + @tbl + N'_CostMethod DEFAULT (N''متوسط مرجح''),
            CostValue      DECIMAL(18,2)      NOT NULL CONSTRAINT DF_' + @tbl + N'_CostValue DEFAULT (0),
            WholesalePrice DECIMAL(18,2)      NOT NULL CONSTRAINT DF_' + @tbl + N'_Wholesale DEFAULT (0),
            IsStock        BIT                NOT NULL CONSTRAINT DF_' + @tbl + N'_IsStock DEFAULT (1),
            IsTaxable      BIT                NOT NULL CONSTRAINT DF_' + @tbl + N'_IsTaxable DEFAULT (0),
            Notes          NVARCHAR(MAX)      NULL
        )
    ');

    -- فهرس فريد على الباركود (فلتر يسمح بقيم NULL المتعددة)
    EXEC(N'
        CREATE UNIQUE INDEX UX_' + @tbl + N'_Barcode
        ON dbo.' + @tbl + N'(Barcode)
        WHERE Barcode IS NOT NULL
    ');

    PRINT N'✔ تم إنشاء جدول [' + @tbl + N'] بالكامل (17 حقل)';
END
ELSE
BEGIN
    /* ---------------------------------------------------------------------------
       2) إضافة الأعمدة الناقصة فقط (بدون لمس الأعمدة الموجودة أو البيانات)
       --------------------------------------------------------------------------- */
    IF COL_LENGTH(N'dbo.' + @tbl, N'Barcode')        IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Barcode        NVARCHAR(100) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Name')           IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Name           NVARCHAR(200) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Price')          IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Price          DECIMAL(18,2) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Unit')           IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Unit           NVARCHAR(50)  NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'EnglishName')    IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD EnglishName    NVARCHAR(200) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'HelperCode')     IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD HelperCode     NVARCHAR(100) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Origin')         IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Origin         NVARCHAR(50)  NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Category')       IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Category       NVARCHAR(100) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Manufacturer')   IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Manufacturer   NVARCHAR(150) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'CompanyCode')    IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD CompanyCode    NVARCHAR(100) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'CostMethod')     IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD CostMethod     NVARCHAR(50)  NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'CostValue')      IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD CostValue      DECIMAL(18,2) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'WholesalePrice') IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD WholesalePrice DECIMAL(18,2) NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'IsStock')        IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD IsStock        BIT           NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'IsTaxable')      IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD IsTaxable      BIT           NULL');
    IF COL_LENGTH(N'dbo.' + @tbl, N'Notes')          IS NULL EXEC(N'ALTER TABLE dbo.' + @tbl + N' ADD Notes          NVARCHAR(MAX) NULL');

    PRINT N'✔ تم إضافة الأعمدة الناقصة إلى جدول [' + @tbl + N']';

    /* ---------------------------------------------------------------------------
       3) تعبئة القيم الافتراضية للصفوف القديمة (الأعمدة الجديدة = NULL)
       --------------------------------------------------------------------------- */
    EXEC(N'
        UPDATE t SET
            t.Unit           = COALESCE(NULLIF(t.Unit, N''''),           N''قطعة''),
            t.CostMethod     = COALESCE(NULLIF(t.CostMethod, N''''),     N''متوسط مرجح''),
            t.Price          = COALESCE(t.Price, 0),
            t.CostValue      = COALESCE(t.CostValue, 0),
            t.WholesalePrice = COALESCE(t.WholesalePrice, 0),
            t.IsStock        = COALESCE(t.IsStock, 1),
            t.IsTaxable      = COALESCE(t.IsTaxable, 0)
        FROM dbo.' + @tbl + N' AS t
    ');

    /* ---------------------------------------------------------------------------
       4) فهرس الباركود (إن لم يكن موجوداً) — يتجاهل أسطر الباركود الفارغ
       --------------------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_' + @tbl + N'_Barcode' AND object_id = OBJECT_ID(N'dbo.' + @tbl))
    BEGIN
        EXEC(N'
            CREATE UNIQUE INDEX UX_' + @tbl + N'_Barcode
            ON dbo.' + @tbl + N'(Barcode)
            WHERE Barcode IS NOT NULL
        ');
        PRINT N'✔ تم إنشاء فهرس فريد على الباركود';
    END
END

/* ---------------------------------------------------------------------------
   5) عرض البنية النهائية للتأكيد
   --------------------------------------------------------------------------- */
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS AS c
WHERE c.TABLE_NAME = @tbl
ORDER BY c.ORDINAL_POSITION;

PRINT N'✔ انتهى السكربت بنجاح';
