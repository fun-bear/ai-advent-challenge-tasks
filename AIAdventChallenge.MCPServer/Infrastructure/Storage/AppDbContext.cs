using Microsoft.EntityFrameworkCore;

namespace AIAdventChallenge.McpServer.Infrastructure.Storage;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskEntry> TaskEntries => Set<TaskEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntry>(entity =>
        {
            entity.ToTable("TaskEntry");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.NextOccurrence)
                .HasConversion(
                    v => v.ToUnixTimeMilliseconds(),
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });    
    }
}