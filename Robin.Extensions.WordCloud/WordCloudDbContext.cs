using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.WordCloud;

internal class WordCloudDbContext(long uin) : DbContext
{
    public DbSet<Record> Records { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=word_cloud-${uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Record>(records =>
        {
            records.HasKey(record => record.RecordId);
            records.Property(record => record.GroupId).IsRequired();
            records.Property(record => record.Content).IsRequired();
        });
    }
}