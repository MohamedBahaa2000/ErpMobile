namespace WebApplication1.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? EnglishName { get; set; }
        public string? Barcode { get; set; }
        public string? HelperCode { get; set; }
        public decimal Price { get; set; }
        public decimal? WholesalePrice { get; set; }
        public decimal? CostValue { get; set; }
        public string Unit { get; set; }
        public string? Category { get; set; }
        public string? Manufacturer { get; set; }
        public string? Origin { get; set; }
        public string? CompanyCode { get; set; }
        public string? CostMethod { get; set; }
        public bool IsStock { get; set; }
        public bool IsTaxable { get; set; }
        public string? Notes { get; set; }
    }
}