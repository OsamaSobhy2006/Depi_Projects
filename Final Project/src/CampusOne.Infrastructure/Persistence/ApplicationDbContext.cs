using CampusOne.Domain.Entities;
using CampusOne.Domain.Entities.AI;
using CampusOne.Domain.Entities.Chat;
using CampusOne.Domain.Entities.Emergency;
using CampusOne.Domain.Entities.LostAndFound;
using CampusOne.Domain.Entities.Sales;
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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

    // Emergency DbSets
    public DbSet<EmergencyIncident> EmergencyIncidents => Set<EmergencyIncident>();
    public DbSet<EmergencyUpdateLog> EmergencyUpdateLogs => Set<EmergencyUpdateLog>();

    // Sales DbSets
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<LostAndFoundItem> LostAndFoundItems => Set<LostAndFoundItem>();
    public DbSet<LostAndFoundClaim> LostAndFoundClaims => Set<LostAndFoundClaim>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();


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

        // Chat Conversation Configuration
        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt).IsRequired();
            entity.HasIndex(c => c.UpdatedAt);
        });

        // Chat Participant Configuration
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

        // Chat Message Configuration
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

        // Sales Configuration
        builder.Entity<ProductCategory>(entity =>
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

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.ImageUrl)
                .HasMaxLength(2048);

            entity.Property(p => p.UserId)
                .HasMaxLength(450);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.UserId);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => new { o.UserId, o.OrderDate });
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Quantity)
                .IsRequired();

            entity.Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LostAndFoundItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReporterUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Category)
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.Location)
                .HasMaxLength(250);

            entity.Property(x => x.ImageUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ReporterUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.Type, x.Status, x.CreatedAt });
        });

        builder.Entity<LostAndFoundClaim>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.ClaimantUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(c => c.VerificationNote)
                .HasMaxLength(2000);

            entity.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne(c => c.LostItem)
                .WithMany(i => i.Claims)
                .HasForeignKey(c => c.LostItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.FoundItem)
                .WithMany()
                .HasForeignKey(c => c.FoundItemId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.ClaimantUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(c => new { c.LostItemId, c.FoundItemId }).IsUnique();
        });
        builder.Entity<Announcement>(b =>
        {
            b.ToTable("Announcements");
            b.HasKey(a => a.Id);
            b.Property(a => a.Title).IsRequired().HasMaxLength(200);
            b.Property(a => a.Content).IsRequired().HasMaxLength(4000);
            b.Property(a => a.Category).HasConversion<int>();
            b.Property(a => a.CreatedByUserId).HasMaxLength(450);
            b.HasIndex(a => new { a.IsPinned, a.PublishedAtUtc });
            b.HasIndex(a => a.Category);
        });
        builder.Entity<Club>(b =>
        {
            b.ToTable("Clubs");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(100);
            b.Property(c => c.Description).IsRequired().HasMaxLength(2000);
            b.Property(c => c.Category).HasConversion<int>();
            b.Property(c => c.LogoUrl).HasMaxLength(500);
            b.Property(c => c.MeetingInfo).HasMaxLength(200);
            b.HasIndex(c => c.Name).IsUnique();
            b.HasIndex(c => c.Category);

            b.HasMany(c => c.Memberships)
                .WithOne(m => m.Club)
                .HasForeignKey(m => m.ClubId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClubMembership>(b =>
        {
            b.ToTable("ClubMemberships");
            b.HasKey(m => m.Id);
            b.Property(m => m.UserId).IsRequired().HasMaxLength(450);
            b.HasIndex(m => new { m.ClubId, m.UserId }).IsUnique();
            b.HasIndex(m => m.UserId);
        });
    }
}
