using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.Infrastructure.Storage;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AgentHistoryEntry> AgentHistoryEntries => Set<AgentHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentHistoryEntry>(entity =>
        {
            entity.ToTable("AgentHistory");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AgentKey).IsRequired();
            entity.Property(x => x.Role).IsRequired();
            entity.Property(x => x.Content).IsRequired();

            entity.HasIndex(x => new { x.AgentKey, x.SortOrder });
        });
    }
}