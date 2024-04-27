using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.UserRank;

// ReSharper disable UnusedAutoPropertyAccessor.Global
internal class UserRankDbContext(long uin) : DbContext
{
    public DbSet<Record> Records { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=user_rank-{uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>(records =>
        {
            records.Property(record => record.GroupId).IsRequired();
            records.Property(record => record.UserId).IsRequired();
        });
    }
}