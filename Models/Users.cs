namespace myFridge.Models
{
    public class Users
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Навігаційне властивість — список продуктів користувача
        public ICollection<Products> Products { get; set; } = new List<Products>();
    }
}
