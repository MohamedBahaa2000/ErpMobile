using SQLite;

namespace ErpMobile.Models;

[Table("Products")]
public sealed class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Barcode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public double Price { get; set; }

    public string Unit { get; set; } = "قطعة";
}
