using Microsoft.EntityFrameworkCore;
using UpCard.Web.Models;

namespace UpCard.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShopRecord> Shops => Set<ShopRecord>();
    public DbSet<CartRecord> Carts => Set<CartRecord>();
    public DbSet<DiscountRuleRecord> DiscountRules => Set<DiscountRuleRecord>();
    public DbSet<AnalyticsEventRecord> AnalyticsEvents => Set<AnalyticsEventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShopRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Domain).IsUnique();
        });

        modelBuilder.Entity<CartRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.Status });
            e.HasOne(x => x.Shop).WithMany(x => x.Carts).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscountRuleRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ShopId);
            e.HasOne(x => x.Shop).WithMany(x => x.DiscountRules).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnalyticsEventRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ShopId, x.EventType, x.CreatedAt });
            e.HasOne(x => x.Shop).WithMany(x => x.Events).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
