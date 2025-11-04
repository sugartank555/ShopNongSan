using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class BatchLot
    {
        public int Id { get; set; }
        [Required, StringLength(40)] public string LotCode { get; set; } = "";
        [Required, StringLength(120)] public string FarmName { get; set; } = "";
        public DateTime HarvestDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        [StringLength(200)] public string? Certification { get; set; }
        public int QuantityIn { get; set; }
        public int QuantitySold { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
