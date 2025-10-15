using Microsoft.EntityFrameworkCore;
using CarDatabase.Models;

namespace CarDatabase.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Car> Cars { get; set; }
    public DbSet<Owner> Owners { get; set; }
    public DbSet<User> Users { get; set; } // ← НОВОЕ!
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Car>()
            .HasOne(c => c.Owner)
            .WithMany(o => o.Cars)
            .HasForeignKey(c => c.OwnerId);
    }
}