/* ============================================================================
   ErpMobile — إنشاء كل جداول المشروع دفعة واحدة
   قاعدة بيانات: SQL Server (T-SQL)  |  خادم الـ API: 192.168.100.70:5034

   الجداول اللي هيتعملها (كلها مطابقة لموديلات التطبيق في ErpMobile/Models):
     1) dbo.Items                 ← نموذج Product        (17 حقل)
     2) dbo.Parties               ← نموذج Party          (14 حقل)
     3) dbo.Invoices              ← نموذج Invoice        (19 حقل)
     4) dbo.InvoiceItems          ← نموذج InvoiceItem    (11 حقل)
     5) dbo.PurchaseInvoices      ← نموذج PurchaseInvoice(19 حقل)
     6) dbo.PurchaseInvoiceItems  ← نموذج PurchaseInvoiceItem (11 حقل)

   خصائص السكربت:
     • قابل للتكرار (Idempotent) — شغّله كذا مرة وما يمسحش بياناتك
     • الجدول مش موجود  → ينشئه بالكامل بكل أعمدته
     • الجدول موجود      → يضيف الأعمدة الناقصة فقط
     • النصوص NVARCHAR   → العربي مايتقطعش
     • الفلوس DECIMAL(18,2) → دقة محاسبية صحيحة (مش float)
     • الكمية DECIMAL(18,3) → يسمح بـ 0.5 كيلو / لتر

   طريقة التشغيل: افتح SSMS ← نافذة استعلام ← الصق الملف كله ← Execute
   ============================================================================ */

SET XACT_ABORT ON;
SET NOCOUNT ON;

/* ===========================================================================
   1) dbo.Items — الأصناف  (17 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.Items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Items
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Barcode        NVARCHAR(100)     NULL,
        Name           NVARCHAR(200)     NOT NULL CONSTRAINT DF_Items_Name DEFAULT (N''),
        Price          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Items_Price DEFAULT (0),
        Unit           NVARCHAR(50)      NOT NULL CONSTRAINT DF_Items_Unit DEFAULT (N'قطعة'),
        EnglishName    NVARCHAR(200)     NULL,
        HelperCode     NVARCHAR(100)     NULL,
        Origin         NVARCHAR(50)      NULL,
        Category       NVARCHAR(100)     NULL,
        Manufacturer   NVARCHAR(150)     NULL,
        CompanyCode    NVARCHAR(100)     NULL,
        CostMethod     NVARCHAR(50)      NOT NULL CONSTRAINT DF_Items_CostMethod DEFAULT (N'متوسط مرجح'),
        CostValue      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Items_CostValue DEFAULT (0),
        WholesalePrice DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Items_Wholesale DEFAULT (0),
        IsStock        BIT               NOT NULL CONSTRAINT DF_Items_IsStock DEFAULT (1),
        IsTaxable      BIT               NOT NULL CONSTRAINT DF_Items_IsTaxable DEFAULT (0),
        Notes          NVARCHAR(MAX)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.Items (17 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Items', N'Barcode')        IS NULL ALTER TABLE dbo.Items ADD Barcode        NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Items', N'Name')           IS NULL ALTER TABLE dbo.Items ADD Name           NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.Items', N'Price')          IS NULL ALTER TABLE dbo.Items ADD Price          DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Items', N'Unit')           IS NULL ALTER TABLE dbo.Items ADD Unit           NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Items', N'EnglishName')    IS NULL ALTER TABLE dbo.Items ADD EnglishName    NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.Items', N'HelperCode')     IS NULL ALTER TABLE dbo.Items ADD HelperCode     NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Items', N'Origin')         IS NULL ALTER TABLE dbo.Items ADD Origin         NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Items', N'Category')       IS NULL ALTER TABLE dbo.Items ADD Category       NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Items', N'Manufacturer')   IS NULL ALTER TABLE dbo.Items ADD Manufacturer   NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Items', N'CompanyCode')    IS NULL ALTER TABLE dbo.Items ADD CompanyCode    NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Items', N'CostMethod')     IS NULL ALTER TABLE dbo.Items ADD CostMethod     NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Items', N'CostValue')      IS NULL ALTER TABLE dbo.Items ADD CostValue      DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Items', N'WholesalePrice') IS NULL ALTER TABLE dbo.Items ADD WholesalePrice DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Items', N'IsStock')        IS NULL ALTER TABLE dbo.Items ADD IsStock        BIT           NULL;
    IF COL_LENGTH(N'dbo.Items', N'IsTaxable')      IS NULL ALTER TABLE dbo.Items ADD IsTaxable      BIT           NULL;
    IF COL_LENGTH(N'dbo.Items', N'Notes')          IS NULL ALTER TABLE dbo.Items ADD Notes          NVARCHAR(MAX) NULL;

    UPDATE dbo.Items SET
        Unit       = COALESCE(NULLIF(Unit, N''),       N'قطعة'),
        CostMethod = COALESCE(NULLIF(CostMethod, N''), N'متوسط مرجح'),
        Price      = COALESCE(Price, 0),
        CostValue  = COALESCE(CostValue, 0),
        WholesalePrice = COALESCE(WholesalePrice, 0),
        IsStock    = COALESCE(IsStock, 1),
        IsTaxable  = COALESCE(IsTaxable, 0);

    PRINT N'  ✔ dbo.Items — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_Items_Barcode' AND object_id = OBJECT_ID(N'dbo.Items'))
BEGIN
    CREATE UNIQUE INDEX UX_Items_Barcode ON dbo.Items(Barcode) WHERE Barcode IS NOT NULL;
    PRINT N'  ✔ فهرس فريد UX_Items_Barcode';
END

/* ===========================================================================
   2) dbo.Parties — العملاء والموردون  (14 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.Parties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Parties
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Type]          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Parties_Type DEFAULT (N'customer'),
        [Code]          NVARCHAR(30)      NULL,
        Name            NVARCHAR(200)     NOT NULL CONSTRAINT DF_Parties_Name DEFAULT (N''),
        EnglishName     NVARCHAR(200)     NULL,
        Phone           NVARCHAR(50)      NULL,
        Mobile          NVARCHAR(50)      NULL,
        [Address]       NVARCHAR(400)     NULL,
        TaxNumber       NVARCHAR(50)      NULL,
        CommercialReg   NVARCHAR(50)      NULL,
        Email           NVARCHAR(150)     NULL,
        PaymentKind     NVARCHAR(20)      NOT NULL CONSTRAINT DF_Parties_Payment DEFAULT (N'نقدي'),
        OpeningBalance  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Parties_Balance DEFAULT (0),
        Notes           NVARCHAR(MAX)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.Parties (14 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Parties', N'[Type]')         IS NULL ALTER TABLE dbo.Parties ADD [Type]         NVARCHAR(20)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Code')           IS NULL ALTER TABLE dbo.Parties ADD [Code]         NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Name')           IS NULL ALTER TABLE dbo.Parties ADD Name           NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.Parties', N'EnglishName')    IS NULL ALTER TABLE dbo.Parties ADD EnglishName    NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Phone')          IS NULL ALTER TABLE dbo.Parties ADD Phone          NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Mobile')         IS NULL ALTER TABLE dbo.Parties ADD Mobile         NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Address')        IS NULL ALTER TABLE dbo.Parties ADD [Address]      NVARCHAR(400) NULL;
    IF COL_LENGTH(N'dbo.Parties', N'TaxNumber')      IS NULL ALTER TABLE dbo.Parties ADD TaxNumber      NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'CommercialReg')  IS NULL ALTER TABLE dbo.Parties ADD CommercialReg  NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Email')          IS NULL ALTER TABLE dbo.Parties ADD Email          NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Parties', N'PaymentKind')    IS NULL ALTER TABLE dbo.Parties ADD PaymentKind    NVARCHAR(20)  NULL;
    IF COL_LENGTH(N'dbo.Parties', N'OpeningBalance') IS NULL ALTER TABLE dbo.Parties ADD OpeningBalance DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Parties', N'Notes')          IS NULL ALTER TABLE dbo.Parties ADD Notes          NVARCHAR(MAX) NULL;

    UPDATE dbo.Parties SET
        [Type]          = COALESCE(NULLIF([Type], N''),       N'customer'),
        PaymentKind     = COALESCE(NULLIF(PaymentKind, N''),  N'نقدي'),
        OpeningBalance  = COALESCE(OpeningBalance, 0);

    PRINT N'  ✔ dbo.Parties — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_Parties_Code' AND object_id = OBJECT_ID(N'dbo.Parties'))
    CREATE UNIQUE INDEX UX_Parties_Code ON dbo.Parties([Code]) WHERE [Code] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Parties_Type' AND object_id = OBJECT_ID(N'dbo.Parties'))
    CREATE INDEX IX_Parties_Type ON dbo.Parties([Type]);

/* ===========================================================================
   3) dbo.Invoices — فواتير المبيعات  (19 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceNumber    INT               NOT NULL CONSTRAINT DF_Invoices_Num DEFAULT (0),
        InvoiceDate      NVARCHAR(10)      NULL,          /* yyyy-MM-dd — بنفس صيغة التطبيق */
        [Store]          NVARCHAR(100)     NOT NULL CONSTRAINT DF_Invoices_Store DEFAULT (N'Main Store'),
        InvoiceType      NVARCHAR(30)      NOT NULL CONSTRAINT DF_Invoices_Type DEFAULT (N'نقدي'),
        Customer         NVARCHAR(200)     NOT NULL CONSTRAINT DF_Invoices_Cust DEFAULT (N'عميل نقدي'),
        SalesRep         NVARCHAR(150)     NOT NULL CONSTRAINT DF_Invoices_Rep  DEFAULT (N'-- اختر --'),
        Driver           NVARCHAR(150)     NOT NULL CONSTRAINT DF_Invoices_Drv  DEFAULT (N'-- اختر --'),
        PriceList        NVARCHAR(100)     NOT NULL CONSTRAINT DF_Invoices_PL   DEFAULT (N'-- اختر --'),
        ApproveJournal   NVARCHAR(30)      NOT NULL CONSTRAINT DF_Invoices_AJ   DEFAULT (N'معتمد'),
        ApproveStoreMove NVARCHAR(30)      NOT NULL CONSTRAINT DF_Invoices_ASM  DEFAULT (N'معتمد'),
        Subtotal         DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_Sub DEFAULT (0),
        ItemDiscount     DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_IDsc DEFAULT (0),
        InvoiceDiscount  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_IDi  DEFAULT (0),
        Vat              DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_Vat  DEFAULT (0),
        Total            DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_Tot  DEFAULT (0),
        Paid             DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_Paid DEFAULT (0),
        Remaining        DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Invoices_Rem  DEFAULT (0),
        DiscountReason   NVARCHAR(300)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.Invoices (19 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.Invoices', N'InvoiceNumber')    IS NULL ALTER TABLE dbo.Invoices ADD InvoiceNumber    INT           NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'InvoiceDate')      IS NULL ALTER TABLE dbo.Invoices ADD InvoiceDate      NVARCHAR(10)  NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Store')            IS NULL ALTER TABLE dbo.Invoices ADD [Store]          NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'InvoiceType')      IS NULL ALTER TABLE dbo.Invoices ADD InvoiceType      NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Customer')         IS NULL ALTER TABLE dbo.Invoices ADD Customer         NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'SalesRep')         IS NULL ALTER TABLE dbo.Invoices ADD SalesRep         NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Driver')           IS NULL ALTER TABLE dbo.Invoices ADD Driver           NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'PriceList')        IS NULL ALTER TABLE dbo.Invoices ADD PriceList        NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'ApproveJournal')   IS NULL ALTER TABLE dbo.Invoices ADD ApproveJournal   NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'ApproveStoreMove') IS NULL ALTER TABLE dbo.Invoices ADD ApproveStoreMove NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Subtotal')         IS NULL ALTER TABLE dbo.Invoices ADD Subtotal         DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'ItemDiscount')     IS NULL ALTER TABLE dbo.Invoices ADD ItemDiscount     DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'InvoiceDiscount')  IS NULL ALTER TABLE dbo.Invoices ADD InvoiceDiscount  DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Vat')              IS NULL ALTER TABLE dbo.Invoices ADD Vat              DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Total')            IS NULL ALTER TABLE dbo.Invoices ADD Total            DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Paid')             IS NULL ALTER TABLE dbo.Invoices ADD Paid             DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'Remaining')        IS NULL ALTER TABLE dbo.Invoices ADD Remaining        DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.Invoices', N'DiscountReason')   IS NULL ALTER TABLE dbo.Invoices ADD DiscountReason   NVARCHAR(300) NULL;

    UPDATE dbo.Invoices SET
        InvoiceNumber    = COALESCE(InvoiceNumber, 0),
        [Store]          = COALESCE(NULLIF([Store], N''),          N'Main Store'),
        InvoiceType      = COALESCE(NULLIF(InvoiceType, N''),      N'نقدي'),
        Customer         = COALESCE(NULLIF(Customer, N''),         N'عميل نقدي'),
        SalesRep         = COALESCE(NULLIF(SalesRep, N''),         N'-- اختر --'),
        Driver           = COALESCE(NULLIF(Driver, N''),           N'-- اختر --'),
        PriceList        = COALESCE(NULLIF(PriceList, N''),        N'-- اختر --'),
        ApproveJournal   = COALESCE(NULLIF(ApproveJournal, N''),   N'معتمد'),
        ApproveStoreMove = COALESCE(NULLIF(ApproveStoreMove, N''), N'معتمد'),
        Subtotal         = COALESCE(Subtotal, 0),
        ItemDiscount     = COALESCE(ItemDiscount, 0),
        InvoiceDiscount  = COALESCE(InvoiceDiscount, 0),
        Vat              = COALESCE(Vat, 0),
        Total            = COALESCE(Total, 0),
        Paid             = COALESCE(Paid, 0),
        Remaining        = COALESCE(Remaining, 0);

    PRINT N'  ✔ dbo.Invoices — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Invoices_Date' AND object_id = OBJECT_ID(N'dbo.Invoices'))
    CREATE INDEX IX_Invoices_Date ON dbo.Invoices(InvoiceDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Invoices_Number' AND object_id = OBJECT_ID(N'dbo.Invoices'))
    CREATE INDEX IX_Invoices_Number ON dbo.Invoices(InvoiceNumber);

/* ===========================================================================
   4) dbo.InvoiceItems — بنود فواتير المبيعات  (11 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.InvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId       INT               NOT NULL,
        Barcode         NVARCHAR(100)     NULL,
        Name            NVARCHAR(200)     NOT NULL CONSTRAINT DF_II_Name DEFAULT (N''),
        Unit            NVARCHAR(50)      NOT NULL CONSTRAINT DF_II_Unit DEFAULT (N'قطعة'),
        Quantity        DECIMAL(18,3)     NOT NULL CONSTRAINT DF_II_Qty   DEFAULT (1),
        Price           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_II_Prc   DEFAULT (0),
        DiscountPercent DECIMAL(18,2)     NOT NULL CONSTRAINT DF_II_DP    DEFAULT (0),
        DiscountAmount  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_II_DA    DEFAULT (0),
        Total           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_II_Tot   DEFAULT (0),
        [Note]          NVARCHAR(400)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.InvoiceItems (11 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.InvoiceItems', N'InvoiceId')       IS NULL ALTER TABLE dbo.InvoiceItems ADD InvoiceId       INT           NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Barcode')         IS NULL ALTER TABLE dbo.InvoiceItems ADD Barcode         NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Name')            IS NULL ALTER TABLE dbo.InvoiceItems ADD Name            NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Unit')            IS NULL ALTER TABLE dbo.InvoiceItems ADD Unit            NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Quantity')        IS NULL ALTER TABLE dbo.InvoiceItems ADD Quantity        DECIMAL(18,3) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Price')           IS NULL ALTER TABLE dbo.InvoiceItems ADD Price           DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'DiscountPercent') IS NULL ALTER TABLE dbo.InvoiceItems ADD DiscountPercent DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'DiscountAmount')  IS NULL ALTER TABLE dbo.InvoiceItems ADD DiscountAmount  DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Total')           IS NULL ALTER TABLE dbo.InvoiceItems ADD Total           DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.InvoiceItems', N'Note')            IS NULL ALTER TABLE dbo.InvoiceItems ADD [Note]          NVARCHAR(400) NULL;

    UPDATE dbo.InvoiceItems SET
        Unit            = COALESCE(NULLIF(Unit, N''), N'قطعة'),
        Quantity        = COALESCE(Quantity, 1),
        Price           = COALESCE(Price, 0),
        DiscountPercent = COALESCE(DiscountPercent, 0),
        DiscountAmount  = COALESCE(DiscountAmount, 0),
        Total           = COALESCE(Total, 0);

    PRINT N'  ✔ dbo.InvoiceItems — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_InvoiceItems_InvoiceId' AND object_id = OBJECT_ID(N'dbo.InvoiceItems'))
    CREATE INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems(InvoiceId);

/* ===========================================================================
   5) dbo.PurchaseInvoices — فواتير المشتريات  (19 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.PurchaseInvoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoices
    (
        Id               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceNumber    INT               NOT NULL CONSTRAINT DF_PI_Num DEFAULT (0),
        InvoiceDate      NVARCHAR(10)      NULL,          /* yyyy-MM-dd */
        [Store]          NVARCHAR(100)     NOT NULL CONSTRAINT DF_PI_Store DEFAULT (N'Main Store'),
        InvoiceType      NVARCHAR(30)      NOT NULL CONSTRAINT DF_PI_Type  DEFAULT (N'نقدي'),
        Supplier         NVARCHAR(200)     NOT NULL CONSTRAINT DF_PI_Sup   DEFAULT (N'مورد نقدي'),
        Rep              NVARCHAR(150)     NOT NULL CONSTRAINT DF_PI_Rep   DEFAULT (N'-- اختر --'),
        Driver           NVARCHAR(150)     NOT NULL CONSTRAINT DF_PI_Drv   DEFAULT (N'-- اختر --'),
        PriceList        NVARCHAR(100)     NOT NULL CONSTRAINT DF_PI_PL    DEFAULT (N'-- اختر --'),
        ApproveJournal   NVARCHAR(30)      NOT NULL CONSTRAINT DF_PI_AJ    DEFAULT (N'معتمد'),
        ApproveStoreMove NVARCHAR(30)      NOT NULL CONSTRAINT DF_PI_ASM   DEFAULT (N'معتمد'),
        Subtotal         DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_Sub   DEFAULT (0),
        ItemDiscount     DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_IDsc  DEFAULT (0),
        InvoiceDiscount  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_IDi   DEFAULT (0),
        Vat              DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_Vat   DEFAULT (0),
        Total            DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_Tot   DEFAULT (0),
        Paid             DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_Paid  DEFAULT (0),
        Remaining        DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PI_Rem   DEFAULT (0),
        DiscountReason   NVARCHAR(300)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.PurchaseInvoices (19 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'InvoiceNumber')    IS NULL ALTER TABLE dbo.PurchaseInvoices ADD InvoiceNumber    INT           NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'InvoiceDate')      IS NULL ALTER TABLE dbo.PurchaseInvoices ADD InvoiceDate      NVARCHAR(10)  NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Store')            IS NULL ALTER TABLE dbo.PurchaseInvoices ADD [Store]          NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'InvoiceType')      IS NULL ALTER TABLE dbo.PurchaseInvoices ADD InvoiceType      NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Supplier')         IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Supplier         NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Rep')              IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Rep              NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Driver')           IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Driver           NVARCHAR(150) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'PriceList')        IS NULL ALTER TABLE dbo.PurchaseInvoices ADD PriceList        NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'ApproveJournal')   IS NULL ALTER TABLE dbo.PurchaseInvoices ADD ApproveJournal   NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'ApproveStoreMove') IS NULL ALTER TABLE dbo.PurchaseInvoices ADD ApproveStoreMove NVARCHAR(30)  NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Subtotal')         IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Subtotal         DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'ItemDiscount')     IS NULL ALTER TABLE dbo.PurchaseInvoices ADD ItemDiscount     DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'InvoiceDiscount')  IS NULL ALTER TABLE dbo.PurchaseInvoices ADD InvoiceDiscount  DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Vat')              IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Vat              DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Total')            IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Total            DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Paid')             IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Paid             DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'Remaining')        IS NULL ALTER TABLE dbo.PurchaseInvoices ADD Remaining        DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoices', N'DiscountReason')   IS NULL ALTER TABLE dbo.PurchaseInvoices ADD DiscountReason   NVARCHAR(300) NULL;

    UPDATE dbo.PurchaseInvoices SET
        InvoiceNumber    = COALESCE(InvoiceNumber, 0),
        [Store]          = COALESCE(NULLIF([Store], N''),          N'Main Store'),
        InvoiceType      = COALESCE(NULLIF(InvoiceType, N''),      N'نقدي'),
        Supplier         = COALESCE(NULLIF(Supplier, N''),         N'مورد نقدي'),
        Rep              = COALESCE(NULLIF(Rep, N''),              N'-- اختر --'),
        Driver           = COALESCE(NULLIF(Driver, N''),           N'-- اختر --'),
        PriceList        = COALESCE(NULLIF(PriceList, N''),        N'-- اختر --'),
        ApproveJournal   = COALESCE(NULLIF(ApproveJournal, N''),   N'معتمد'),
        ApproveStoreMove = COALESCE(NULLIF(ApproveStoreMove, N''), N'معتمد'),
        Subtotal         = COALESCE(Subtotal, 0),
        ItemDiscount     = COALESCE(ItemDiscount, 0),
        InvoiceDiscount  = COALESCE(InvoiceDiscount, 0),
        Vat              = COALESCE(Vat, 0),
        Total            = COALESCE(Total, 0),
        Paid             = COALESCE(Paid, 0),
        Remaining        = COALESCE(Remaining, 0);

    PRINT N'  ✔ dbo.PurchaseInvoices — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_PurchaseInvoices_Date' AND object_id = OBJECT_ID(N'dbo.PurchaseInvoices'))
    CREATE INDEX IX_PurchaseInvoices_Date ON dbo.PurchaseInvoices(InvoiceDate);

/* ===========================================================================
   6) dbo.PurchaseInvoiceItems — بنود فواتير المشتريات  (11 حقل)
   =========================================================================== */
IF OBJECT_ID(N'dbo.PurchaseInvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoiceItems
    (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId       INT               NOT NULL,
        Barcode         NVARCHAR(100)     NULL,
        Name            NVARCHAR(200)     NOT NULL CONSTRAINT DF_PII_Name DEFAULT (N''),
        Unit            NVARCHAR(50)      NOT NULL CONSTRAINT DF_PII_Unit DEFAULT (N'قطعة'),
        Quantity        DECIMAL(18,3)     NOT NULL CONSTRAINT DF_PII_Qty  DEFAULT (1),
        Price           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PII_Prc  DEFAULT (0),
        DiscountPercent DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PII_DP   DEFAULT (0),
        DiscountAmount  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PII_DA   DEFAULT (0),
        Total           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PII_Tot  DEFAULT (0),
        [Note]          NVARCHAR(400)     NULL
    );
    PRINT N'  ✔ تم إنشاء dbo.PurchaseInvoiceItems (11 حقل)';
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'InvoiceId')       IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD InvoiceId       INT           NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Barcode')         IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Barcode         NVARCHAR(100) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Name')            IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Name            NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Unit')            IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Unit            NVARCHAR(50)  NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Quantity')        IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Quantity        DECIMAL(18,3) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Price')           IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Price           DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'DiscountPercent') IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD DiscountPercent DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'DiscountAmount')  IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD DiscountAmount  DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Total')           IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD Total           DECIMAL(18,2) NULL;
    IF COL_LENGTH(N'dbo.PurchaseInvoiceItems', N'Note')            IS NULL ALTER TABLE dbo.PurchaseInvoiceItems ADD [Note]          NVARCHAR(400) NULL;

    UPDATE dbo.PurchaseInvoiceItems SET
        Unit            = COALESCE(NULLIF(Unit, N''), N'قطعة'),
        Quantity        = COALESCE(Quantity, 1),
        Price           = COALESCE(Price, 0),
        DiscountPercent = COALESCE(DiscountPercent, 0),
        DiscountAmount  = COALESCE(DiscountAmount, 0),
        Total           = COALESCE(Total, 0);

    PRINT N'  ✔ dbo.PurchaseInvoiceItems — تمت إضافة الأعمدة الناقصة';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_PurchaseInvoiceItems_InvoiceId' AND object_id = OBJECT_ID(N'dbo.PurchaseInvoiceItems'))
    CREATE INDEX IX_PurchaseInvoiceItems_InvoiceId ON dbo.PurchaseInvoiceItems(InvoiceId);

/* ===========================================================================
   7) العلاقات (Foreign Keys)
      البنود لازم يكون ليها فاتورة أم (Integrity).
      لو الـ API بتاعك بينزّل البنود بـ InvoiceId غير موجود → ممكن يفشل.
      في الحالة دي شيل السطر المناسب:
      ALTER TABLE dbo.InvoiceItems DROP CONSTRAINT FK_InvoiceItems_Invoices;
   =========================================================================== */
IF OBJECT_ID(N'dbo.FK_InvoiceItems_Invoices', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceItems WITH CHECK
        ADD CONSTRAINT FK_InvoiceItems_Invoices
        FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id);
    PRINT N'  ✔ العلاقة FK_InvoiceItems_Invoices';
END

IF OBJECT_ID(N'dbo.FK_PurchaseInvoiceItems_PurchaseInvoices', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseInvoiceItems WITH CHECK
        ADD CONSTRAINT FK_PurchaseInvoiceItems_PurchaseInvoices
        FOREIGN KEY (InvoiceId) REFERENCES dbo.PurchaseInvoices(Id);
    PRINT N'  ✔ العلاقة FK_PurchaseInvoiceItems_PurchaseInvoices';
END

/* ===========================================================================
   8) تقرير نهائي: كل الجداول وأعداد أعمدتها
   =========================================================================== */
SELECT
    t.name                          AS [الجدول],
    p.rows                          AS [عدد الصفوف],
    COUNT(c.column_id)              AS [عدد الأعمدة]
FROM sys.tables  AS t
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
JOIN sys.columns  AS c ON c.object_id = t.object_id
WHERE t.name IN (N'Items', N'Parties', N'Invoices', N'InvoiceItems',
                 N'PurchaseInvoices', N'PurchaseInvoiceItems')
GROUP BY t.name, p.rows
ORDER BY t.name;

PRINT N'';
PRINT N'✔ ✔ ✔ انتهى السكربت بنجاح — كل جداول المشروع جاهزة';
PRINT N'   ملاحظة: عشان الـ API يرجّع الأعمدة الجديدة لازم تزودها في';
PRINT N'   الـ Entity/DTO والـ Controller بتوعك في مشروع الـ API.';

/* --------------------------------------------------------------------------
   مثال: بنود جدول معين (شيل التعليق عشان تشوف الأعمدة)
   --------------------------------------------------------------------------
   SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
   FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Parties'
   ORDER BY ORDINAL_POSITION;
   -------------------------------------------------------------------------- */
