using System.ComponentModel.DataAnnotations.Schema;

namespace myFridge.Models
{
    [Table("products")]
    public class Products
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Users User { get; set; } = null!;

        public Guid? StoragePlaceId { get; set; }
        public StoragePlace? StoragePlace { get; set; }

        public string Name { get; set; } = null!;
        public decimal Quantity { get; set; } = 1;
        public UnitType Unit { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
