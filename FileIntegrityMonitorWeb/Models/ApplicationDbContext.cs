// Models/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileIntegrityMonitor.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileRecord> FileRecords { get; set; }
        public DbSet<FileChangeLog> FileChangeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the FileRecord entity
            builder.Entity<FileRecord>()
                .HasIndex(f => f.FilePath)
                .IsUnique();

            // Configure the FileChangeLog entity
            builder.Entity<FileChangeLog>()
                .HasOne(l => l.User)
                .WithMany(u => u.FileChanges)
                .HasForeignKey(l => l.UserId);

            builder.Entity<FileChangeLog>()
                .HasOne(l => l.FileRecord)
                .WithMany(f => f.ChangeLogs)
                .HasForeignKey(l => l.FileRecordId);
        }
    }
}