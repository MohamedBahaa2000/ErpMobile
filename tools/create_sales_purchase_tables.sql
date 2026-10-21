/* ═══════════════════════════════════════════════════════════════════
   جداول المبيعات والمشتريات — ERP_Db
   ───────────────────────────────────────────────────────────────────
   ► شغّل هذا الملف في SSMS على جهاز 192.168.100.70
   ► آمن للتكرار (IF NOT EXISTS) — تشغّله مرة أو أكثر

   الجداول الأربعة:
     dbo.Invoices             فواتير المبيعات
     dbo.InvoiceItems         بنود فواتير المبيعات
     dbo.PurchaseInvoices     فواتير المشتريات
     dbo.PurchaseInvoiceItems بنود فواتير المشتريات

   ⚠️ ملاحظة مهمة: عمود الربط بين الفاتورة وبنودها اسمه
      «InvoiceId» في الجداول الأربعة (نفس اسم موديل التطبيق).
   ═══════════════════════════════════════════════════════════════════ */

USE ERP_Db;
GO

/* ═════════════════════════ 1) فواتير المبيعات ═════════════════════════ */
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id                 INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber      INT           NOT NULL CONSTRAINT DF_Invoices_Num DEFAULT (0),
        InvoiceDate        NVARCHAR(10)  NOT NULL CONSTRAINT DF_Invoices_Date DEFAULT (CONVERT(NVARCHAR(10), GETDATE(), 120)),
        Store              NVARCHAR(100) NOT NULL CONSTRAINT DF_Invoices_Store DEFAULT (N'Main Store'),
        InvoiceType        NVARCHAR(50)  NOT NULL CONSTRAINT DF_Invoices_Type  DEFAULT (N'نقدي'),
        Customer           NVARCHAR(200) NOT NULL CONSTRAINT DF_Invoices_Cust  DEFAULT (N'عميل نقدي'),
        SalesRep           NVARCHAR(200) NOT NULL CONSTRAINT DF_Invoices_Rep   DEFAULT (N'-- اختر --'),
        Driver             NVARCHAR(200) NOT NULL CONSTRAINT DF_Invoices_Drv   DEFAULT (N'-- اختر --'),
        PriceList          NVARCHAR(200) NOT NULL CONSTRAINT DF_Invoices_PL    DEFAULT (N'-- اختر --'),
        ApproveJournal     NVARCHAR(50)  NOT NULL CONSTRAINT DF_Invoices_AJ    DEFAULT (N'معتمد'),
        ApproveStoreMove   NVARCHAR(50)  NOT NULL CONSTRAINT DF_Invoices_ASM   DEFAULT (N'معتمد'),
        Subtotal           DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Sub   DEFAULT (0),
        ItemDiscount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_IDisc DEFAULT (0),
        InvoiceDiscount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_IDisc2 DEFAULT (0),
        Vat                DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Vat   DEFAULT (0),
        Total              DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Tot   DEFAULT (0),
        Paid               DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Paid  DEFAULT (0),
        Remaining          DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Rem   DEFAULT (0),
        DiscountReason     NVARCHAR(300) NULL
    );

    CREATE UNIQUE INDEX UX_Invoices_Number ON dbo.Invoices(InvoiceNumber);
    CREATE INDEX IX_Invoices_Date ON dbo.Invoices(InvoiceDate);

    PRINT '✅ تم إنشاء جدول dbo.Invoices';
END
ELSE
    PRINT '• جدول dbo.Invoices موجود مسبقاً';
GO

/* ═════════════════════════ 2) بنود فواتير المبيعات ═════════════════════════ */
IF OBJECT_ID(N'dbo.InvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems
    (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId        INT             NOT NULL,
        Barcode          NVARCHAR(100)   NULL,
        Name             NVARCHAR(300)   NOT NULL,
        Unit             NVARCHAR(50)    NOT NULL CONSTRAINT DF_II_Unit  DEFAULT (N'قطعة'),
        Quantity         DECIMAL(18,3)   NOT NULL CONSTRAINT DF_II_Qty   DEFAULT (1),
        Price            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_II_Price DEFAULT (0),
        DiscountPercent  DECIMAL(9,4)    NOT NULL CONSTRAINT DF_II_DP    DEFAULT (0),
        DiscountAmount   DECIMAL(18,2)   NOT NULL CONSTRAINT DF_II_DA    DEFAULT (0),
        Total            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_II_Tot   DEFAULT (0),
        Note             NVARCHAR(500)   NULL,

        CONSTRAINT FK_InvoiceItems_Invoices
            FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems(InvoiceId);
    CREATE INDEX IX_InvoiceItems_Barcode   ON dbo.InvoiceItems(Barcode);

    PRINT '✅ تم إنشاء جدول dbo.InvoiceItems';
END
ELSE
    PRINT '• جدول dbo.InvoiceItems موجود مسبقاً';
GO

/* ═════════════════════════ 3) فواتير المشتريات ═════════════════════════ */
IF OBJECT_ID(N'dbo.PurchaseInvoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoices
    (
        Id                 INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber      INT           NOT NULL CONSTRAINT DF_PI_Num  DEFAULT (0),
        InvoiceDate        NVARCHAR(10)  NOT NULL CONSTRAINT DF_PI_Date DEFAULT (CONVERT(NVARCHAR(10), GETDATE(), 120)),
        Store              NVARCHAR(100) NOT NULL CONSTRAINT DF_PI_Store DEFAULT (N'Main Store'),
        InvoiceType        NVARCHAR(50)  NOT NULL CONSTRAINT DF_PI_Type  DEFAULT (N'نقدي'),
        Supplier           NVARCHAR(200) NOT NULL CONSTRAINT DF_PI_Sup   DEFAULT (N'مورد نقدي'),
        Rep                NVARCHAR(200) NOT NULL CONSTRAINT DF_PI_Rep   DEFAULT (N'-- اختر --'),
        Driver             NVARCHAR(200) NOT NULL CONSTRAINT DF_PI_Drv   DEFAULT (N'-- اختر --'),
        PriceList          NVARCHAR(200) NOT NULL CONSTRAINT DF_PI_PL    DEFAULT (N'-- اختر --'),
        ApproveJournal     NVARCHAR(50)  NOT NULL CONSTRAINT DF_PI_AJ    DEFAULT (N'معتمد'),
        ApproveStoreMove   NVARCHAR(50)  NOT NULL CONSTRAINT DF_PI_ASM   DEFAULT (N'معتمد'),
        Subtotal           DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_Sub   DEFAULT (0),
        ItemDiscount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_IDisc DEFAULT (0),
        InvoiceDiscount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_IDisc2 DEFAULT (0),
        Vat                DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_Vat   DEFAULT (0),
        Total              DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_Tot   DEFAULT (0),
        Paid               DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_Paid  DEFAULT (0),
        Remaining          DECIMAL(18,2) NOT NULL CONSTRAINT DF_PI_Rem   DEFAULT (0),
        DiscountReason     NVARCHAR(300) NULL
    );

    CREATE UNIQUE INDEX UX_PurchaseInvoices_Number ON dbo.PurchaseInvoices(InvoiceNumber);
    CREATE INDEX IX_PurchaseInvoices_Date ON dbo.PurchaseInvoices(InvoiceDate);

    PRINT '✅ تم إنشاء جدول dbo.PurchaseInvoices';
END
ELSE
    PRINT '• جدول dbo.PurchaseInvoices موجود مسبقاً';
GO

/* ═════════════════════════ 4) بنود فواتير المشتريات ═════════════════════════ */
IF OBJECT_ID(N'dbo.PurchaseInvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoiceItems
    (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId        INT             NOT NULL,
        Barcode          NVARCHAR(100)   NULL,
        Name             NVARCHAR(300)   NOT NULL,
        Unit             NVARCHAR(50)    NOT NULL CONSTRAINT DF_PII_Unit  DEFAULT (N'قطعة'),
        Quantity         DECIMAL(18,3)   NOT NULL CONSTRAINT DF_PII_Qty   DEFAULT (1),
        Price            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PII_Price DEFAULT (0),
        DiscountPercent  DECIMAL(9,4)    NOT NULL CONSTRAINT DF_PII_DP    DEFAULT (0),
        DiscountAmount   DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PII_DA    DEFAULT (0),
        Total            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_PII_Tot   DEFAULT (0),
        Note             NVARCHAR(500)   NULL,

        CONSTRAINT FK_PurchaseInvoiceItems_PurchaseInvoices
            FOREIGN KEY (InvoiceId) REFERENCES dbo.PurchaseInvoices(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_PurchaseInvoiceItems_InvoiceId ON dbo.PurchaseInvoiceItems(InvoiceId);
    CREATE INDEX IX_PurchaseInvoiceItems_Barcode   ON dbo.PurchaseInvoiceItems(Barcode);

    PRINT '✅ تم إنشاء جدول dbo.PurchaseInvoiceItems';
END
ELSE
    PRINT '• جدول dbo.PurchaseInvoiceItems موجود مسبقاً';
GO

/* ═════════════════════════ تقرير نهائي ═════════════════════════ */
PRINT '';
PRINT '=== حالة الجداول والسجلات ===';
SELECT
    t.name                              AS TableName,
    SUM(p.rows)                         AS [Rows],
    COUNT(c.column_id)                  AS Columns
FROM sys.tables  t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
JOIN sys.columns  c ON c.object_id = t.object_id
WHERE t.name IN (N'Invoices', N'InvoiceItems', N'PurchaseInvoices', N'PurchaseInvoiceItems')
GROUP BY t.name
ORDER BY t.name;
GO
