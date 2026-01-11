using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.Seiyuu;

public class SeiyuuDbContext(long uin = 0) : DbContext
{
    public DbSet<SeiyuuAlias> Aliases { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite($"Data Source=seiyuu-{uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SeiyuuAlias>(aliases =>
        {
            aliases.HasKey(alias => alias.From);
            aliases.Property(alias => alias.To).IsRequired();
        });
    }
}
