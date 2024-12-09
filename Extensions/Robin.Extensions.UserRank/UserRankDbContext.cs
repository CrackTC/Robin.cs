using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.UserRank;

internal class UserRankDbContext(long uin = 0) : DbContext
{
    public DbSet<Member> Members { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite($"Data Source=user_rank-{uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(members =>
        {
            members.HasKey(member => new { member.GroupId, member.UserId });
            members.Property(member => member.Count).IsRequired();
            members.Property(member => member.Timestamp).IsRequired();
            members.Property(member => member.PrevCount).IsRequired();
            members.Property(member => member.PrevTimestamp).IsRequired();
        });
    }
}
