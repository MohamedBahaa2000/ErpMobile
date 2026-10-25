namespace WebApplication1.Models
{
    public class Party
    {
        public int Id { get; set; }
        public string Type { get; set; } = "customer";
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? EnglishName { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? CommercialReg { get; set; }
        public string? Email { get; set; }
        public string PaymentKind { get; set; } = "نقدي";
        public decimal OpeningBalance { get; set; } = 0;
        public string? Notes { get; set; }
    }
}