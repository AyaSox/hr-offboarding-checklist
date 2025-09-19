using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OffboardingChecklist.Models;

namespace OffboardingChecklist.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<OffboardingProcess> OffboardingProcesses { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<OffboardingDocument> OffboardingDocuments { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<TaskTemplate> TaskTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RowVersion for different database providers
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // For SQLite, use a regular byte array that we manage manually
                modelBuilder.Entity<OffboardingProcess>()
                    .Property(e => e.RowVersion)
                    .IsRowVersion()
                    .HasDefaultValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
                    .ValueGeneratedOnAddOrUpdate();

                modelBuilder.Entity<ChecklistItem>()
                    .Property(e => e.RowVersion)
                    .IsRowVersion()
                    .HasDefaultValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
                    .ValueGeneratedOnAddOrUpdate();
            }
            else
            {
                // For SQL Server, use the standard rowversion
                modelBuilder.Entity<OffboardingProcess>()
                    .Property(e => e.RowVersion)
                    .IsRowVersion();

                modelBuilder.Entity<ChecklistItem>()
                    .Property(e => e.RowVersion)
                    .IsRowVersion();
            }

            // Configure OffboardingProcess relationships
            modelBuilder.Entity<OffboardingProcess>()
                .HasMany(p => p.ChecklistItems)
                .WithOne(c => c.OffboardingProcess)
                .HasForeignKey(c => c.OffboardingProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OffboardingProcess>()
                .HasMany(p => p.Documents)
                .WithOne(d => d.OffboardingProcess)
                .HasForeignKey(d => d.OffboardingProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ChecklistItem relationships
            modelBuilder.Entity<ChecklistItem>()
                .HasMany(c => c.TaskComments)
                .WithOne(tc => tc.ChecklistItem)
                .HasForeignKey(tc => tc.ChecklistItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure task dependencies
            modelBuilder.Entity<ChecklistItem>()
                .HasOne(c => c.DependsOnTask)
                .WithMany(c => c.DependentTasks)
                .HasForeignKey(c => c.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TaskTemplate dependencies
            modelBuilder.Entity<TaskTemplate>()
                .HasOne(t => t.DependsOnTemplate)
                .WithMany(t => t.DependentTemplates)
                .HasForeignKey(t => t.DependsOnTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Notifications table without foreign key relationships initially
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title).IsRequired();
                entity.Property(n => n.Message).IsRequired();
                entity.Property(n => n.RecipientUserId).IsRequired();
                
                // No foreign key relationships to avoid cascade conflicts
                entity.Ignore(n => n.RelatedProcess);
                entity.Ignore(n => n.RelatedTask);
                
                // Configure enum conversions
                entity.Property(n => n.Type).HasConversion<int>();
                entity.Property(n => n.Priority).HasConversion<int>();
                
                // Configure indexes for better query performance
                entity.HasIndex(n => n.RecipientUserId);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.Type);
                entity.HasIndex(n => n.CreatedOn);
            });

            // Configure Department unique constraint
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // Configure indexes for better performance
            modelBuilder.Entity<OffboardingProcess>()
                .HasIndex(p => p.Status);
            modelBuilder.Entity<OffboardingProcess>()
                .HasIndex(p => p.StartDate);
            modelBuilder.Entity<OffboardingProcess>()
                .HasIndex(p => p.IsClosed);

            modelBuilder.Entity<ChecklistItem>()
                .HasIndex(c => c.IsCompleted);
            modelBuilder.Entity<ChecklistItem>()
                .HasIndex(c => c.Department);
            modelBuilder.Entity<ChecklistItem>()
                .HasIndex(c => c.DueDate);

            modelBuilder.Entity<TaskComment>()
                .HasIndex(tc => tc.CreatedOn);

            modelBuilder.Entity<TaskTemplate>()
                .HasIndex(tt => tt.Department);
        }

        public override int SaveChanges()
        {
            UpdateRowVersions();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateRowVersions();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateRowVersions()
        {
            // For SQLite, manually update RowVersion values
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                var entities = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .Where(e => e.Entity is OffboardingProcess || e.Entity is ChecklistItem);

                foreach (var entity in entities)
                {
                    var rowVersionProperty = entity.Property("RowVersion");
                    if (rowVersionProperty != null)
                    {
                        // Generate a new timestamp-like value for SQLite
                        var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
                        rowVersionProperty.CurrentValue = timestamp;
                    }
                }
            }
        }
    }
}
