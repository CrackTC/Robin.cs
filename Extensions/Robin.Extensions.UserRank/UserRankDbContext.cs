using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.UserRank;

internal class UserRankDbContext(long uin = 0) : DbContext
{
    public DbSet<Record> Records { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseModel(CompiledModels.UserRankDbContextModel.Instance)
            .UseSqlite($"Data Source=user_rank-{uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>(records =>
        {
            records.HasKey(record => record.RecordId);
            records.Property(record => record.GroupId).IsRequired();
            records.Property(record => record.UserId).IsRequired();
        });
    }
}
