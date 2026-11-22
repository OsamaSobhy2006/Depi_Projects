using CampusOne.Domain.Entities;
using CampusOne.Domain.Entities.AI;
using CampusOne.Domain.Entities.Emergency;
using CampusOne.Domain.Entities.UniversityServices;
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
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<StudyGroupMember> StudyGroupMembers => Set<StudyGroupMember>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<EmergencyIncident> EmergencyIncidents => Set<EmergencyIncident>();
    public DbSet<EmergencyUpdateLog> EmergencyUpdateLogs => Set<EmergencyUpdateLog>();

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

        // StudyGroup Configuration
        builder.Entity<StudyGroup>(entity =>
        {
            entity.HasKey(g => g.Id);

            entity.Property(g => g.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(g => g.CourseCode)
                .HasMaxLength(30);

            entity.Property(g => g.Description)
                .HasMaxLength(1000);

            entity.Property(g => g.CreatedByUserId)
                .IsRequired();

            entity.Property(g => g.CreatedAt)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(g => g.CourseCode);
            entity.HasIndex(g => g.CreatedAt);
        });

        // StudyGroupMember Configuration
        builder.Entity<StudyGroupMember>(entity =>
        {
            entity.HasKey(m => new { m.StudyGroupId, m.UserId });

            entity.Property(m => m.UserId)
                .IsRequired();

            entity.Property(m => m.Role)
                .IsRequired();

            entity.Property(m => m.JoinedAt)
                .IsRequired();

            entity.HasOne(m => m.StudyGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(m => m.UserId);
        });

        // StudySession Configuration
        builder.Entity<StudySession>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Topic)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.RoomCode)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.IsLive)
                .IsRequired();

            entity.Property(s => s.StartedByUserId)
                .IsRequired();

            entity.Property(s => s.StartedAt)
                .IsRequired();

            entity.HasOne(s => s.StudyGroup)
                .WithMany(g => g.Sessions)
                .HasForeignKey(s => s.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.StartedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(s => s.RoomCode);
            entity.HasIndex(s => new { s.StudyGroupId, s.IsLive });
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

        // Emergency / SOS Configuration
        builder.Entity<EmergencyIncident>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReporterUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.AssignedResponderId)
                .HasMaxLength(450);

            entity.Property(e => e.Category)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.AiSuggestedCategory)
                .HasMaxLength(100);

            entity.Property(e => e.AiStructuredSummary)
                .HasMaxLength(2000);

            entity.Property(e => e.LocationLabel)
                .HasMaxLength(250);

            entity.Property(e => e.ResolutionNotes)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.ReporterUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.AssignedResponderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(e => e.Updates)
                .WithOne(u => u.Incident)
                .HasForeignKey(u => u.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.ReporterUserId);
        });

        builder.Entity<EmergencyUpdateLog>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.ActorUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(u => u.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(40);

            entity.Property(u => u.Message)
                .HasMaxLength(2000);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(u => u.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(u => new { u.IncidentId, u.CreatedAt });
        });
    }
}