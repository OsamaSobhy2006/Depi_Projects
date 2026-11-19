using CampusOne.Domain.Entities.AI;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AiConversation> AiConversations => Set<AiConversation>();

    public DbSet<AiMessage> AiMessages => Set<AiMessage>();

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

       

        // University Services Configuration

        builder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            entity.Property(c => c.Icon)
                .HasMaxLength(100);

            entity.HasIndex(c => c.Name)
                .IsUnique();
        });

        builder.Entity<ServiceItem>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(s => s.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(s => s.Instructions)
                .HasMaxLength(4000);

            entity.Property(s => s.Icon)
                .HasMaxLength(100);

            entity.HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.CategoryId, s.Name })
                .IsUnique();
        });

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.RequestNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(r => r.StudentId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(r => r.Subject)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(3000);

            entity.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(r => r.Priority)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(r => r.AdminNotes)
                .HasMaxLength(2000);

            entity.Property(r => r.AssignedTo)
                .HasMaxLength(450);

            entity.HasOne(r => r.ServiceItem)
                .WithMany(s => s.Requests)
                .HasForeignKey(r => r.ServiceItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.RequestNumber)
                .IsUnique();

            entity.HasIndex(r => new { r.StudentId, r.SubmittedAt });
        });

    }
}
