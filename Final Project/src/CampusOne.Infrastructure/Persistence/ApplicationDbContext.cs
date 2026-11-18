using CampusOne.Domain.Entities;
using CampusOne.Domain.Entities.AI;
using CampusOne.Domain.Entities.Chat;
using CampusOne.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BorrowRequest> BorrowRequests => Set<BorrowRequest>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Book Configuration
        builder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.Author)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(b => b.Cover)
                .HasMaxLength(500);

            entity.Property(b => b.Description)
                .HasMaxLength(1000);

            entity.Property(b => b.FileUrl)
                .HasMaxLength(2048);
        });

        // BorrowRequest Configuration
        builder.Entity<BorrowRequest>(entity =>
        {
            entity.HasKey(br => br.Id);

            entity.Property(br => br.UserId)
                .IsRequired();

            entity.Property(br => br.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(br => br.FineAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(br => br.Book)
                .WithMany()
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(br => br.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AI Conversation Configuration
        builder.Entity<AiConversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.UserId)
                .IsRequired();

            entity.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(c => c.CreatedAt)
                .IsRequired();

            entity.Property(c => c.UpdatedAt)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.UpdatedAt);
        });

        // AI Message Configuration
        builder.Entity<AiMessage>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Sender)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(m => m.Content)
                .IsRequired();

            entity.Property(m => m.CreatedAt)
                .IsRequired();
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt).IsRequired();
            entity.HasIndex(c => c.UpdatedAt);
        });

        builder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(p => new
            {
                p.ConversationId,
                p.UserId
            });
            entity.Property(p => p.UserId).IsRequired();

            entity.HasOne(p => p.Conversation)
               .WithMany(c => c.Participants)
               .HasForeignKey(p => p.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(p => p.UserId);
        });

        builder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.SenderId)
                .IsRequired();

            entity.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(m => m.CreatedAt)
                .IsRequired();

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(m => new
            {
                m.ConversationId,
                m.CreatedAt
            });
        });
    }
}
