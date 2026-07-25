using Microsoft.EntityFrameworkCore;
using store.Models;


namespace store.Data
{
    public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
    {
        public DbSet<Users> Users => Set<Users>();

        public DbSet<Products> Products => Set<Products>();

        public DbSet<Carts> Carts => Set<Carts>();

        public DbSet<Cart_Items> Cart_Items => Set<Cart_Items>();
        public DbSet<ChatMessages> ChatMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>()
                .Property(u => u.Role)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }

}


