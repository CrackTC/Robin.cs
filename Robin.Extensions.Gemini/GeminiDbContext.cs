using Microsoft.EntityFrameworkCore;

namespace Robin.Extensions.Gemini;

// ReSharper disable UnusedAutoPropertyAccessor.Global
internal class GeminiDbContext(long uin) : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=gemini-{uin}.db");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(users =>
        {
            users.HasKey(user => user.UserId);
            users.Property(user => user.ModelName).IsRequired();
            users.Property(user => user.SystemCommand).IsRequired();
            users.HasMany(user => user.Messages).WithOne(msg => msg.User);
        });

        model.Entity<Message>(messages =>
        {
            messages.Property(msg => msg.Role).IsRequired();
            messages.Property(msg => msg.Content).IsRequired();
            messages.Property(msg => msg.Timestamp).IsRequired();
        });
    }
}