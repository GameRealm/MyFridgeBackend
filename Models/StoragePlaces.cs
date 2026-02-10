namespace myFridge.Models
{
    public class StoragePlace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Навігаційне властивість — список продуктів у цьому сховищі
        public ICollection<Products> Products { get; set; } = new List<Products>();
    }
}
