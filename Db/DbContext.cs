using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using myFridge.Models;
namespace myFridge.Db;

public class myFridgeDb : DbContext
{
    public myFridgeDb(DbContextOptions<myFridgeDb> options) : base(options) { }
    public DbSet<Users> Users { get; set; }
    public DbSet<Products> Products { get; set; }
    public DbSet<StoragePlace> StoragePlaces { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UsersConfiguration());
        modelBuilder.ApplyConfiguration(new ProductsConfiguration());
        modelBuilder.ApplyConfiguration(new StoragePlacesConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

public class UsersConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Email).HasColumnName("email").HasColumnType("varchar(255)").IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("now()");
    }
}

public class StoragePlacesConfiguration : IEntityTypeConfiguration<StoragePlace>
{
    public void Configure(EntityTypeBuilder<StoragePlace> builder)
    {
        builder.ToTable("storage_places");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(sp => sp.Name).HasColumnName("name").HasColumnType("varchar(100)").IsRequired();

        builder.Property(sp => sp.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("now()");
    }
}

public class ProductsConfiguration : IEntityTypeConfiguration<Products>
{
    public void Configure(EntityTypeBuilder<Products> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        // Foreign key: User
        builder.Property(p => p.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.HasOne(p => p.User).WithMany(u => u.Products).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        // Foreign key: StoragePlace
        builder.Property(p => p.StoragePlaceId).HasColumnName("storage_place_id").HasColumnType("uuid");
        builder.HasOne(p => p.StoragePlace).WithMany(sp => sp.Products).HasForeignKey(p => p.StoragePlaceId);

        builder.Property(p => p.Name).HasColumnName("name").HasColumnType("varchar(255)").IsRequired();

        builder.Property(p => p.Quantity).HasColumnName("quantity").HasColumnType("numeric(10,2)").HasDefaultValue(1).IsRequired();

        builder.Property(p => p.Unit).HasColumnName("unit").HasConversion<string>() .HasColumnType("text").IsRequired();

        builder.Property(p => p.ExpirationDate).HasColumnName("expiration_date").HasColumnType("date");

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("now()");
    }
}