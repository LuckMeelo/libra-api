using LibrApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrApi.Data
{
    public class LibrApiDbContext : DbContext
    {
        public LibrApiDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("libra");
        }

        public DbSet<Book> Books { get; set; }
    }
}
