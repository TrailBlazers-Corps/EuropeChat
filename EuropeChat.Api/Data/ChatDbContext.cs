using Microsoft.EntityFrameworkCore;
using EuropeChat.Models;

namespace EuropeChat.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.HasMany(e => e.Messages)
                  .WithOne()
                  .HasForeignKey(m => m.ConversationId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.Role).HasConversion<string>();
        });
    }
}

