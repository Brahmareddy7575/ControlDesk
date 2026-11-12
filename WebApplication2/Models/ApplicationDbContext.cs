using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }

        public DbSet<product> Products { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ApprovalStatus> ApprovalStatuses { get; set; }
        public DbSet<RegistrationRequest> RegistrationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.RoleName).IsUnique();
                entity.HasData(
                    new Role
                    {
                        RoleId = 1,
                        RoleName = "Admin",
                        Description = "Administrator with full user approval and product management access"
                    },
                    new Role
                    {
                        RoleId = 2,
                        RoleName = "User",
                        Description = "Standard user with personal dashboard and read-only product access"
                    }
                );
            });

            modelBuilder.Entity<ApprovalStatus>(entity =>
            {
                entity.HasIndex(s => s.StatusName).IsUnique();
                entity.HasData(
                    new ApprovalStatus
                    {
                        ApprovalStatusId = 1,
                        StatusName = "Pending",
                        Description = "Waiting for Admin review"
                    },
                    new ApprovalStatus
                    {
                        ApprovalStatusId = 2,
                        StatusName = "Approved",
                        Description = "Approved by Admin"
                    },
                    new ApprovalStatus
                    {
                        ApprovalStatusId = 3,
                        StatusName = "Rejected",
                        Description = "Rejected by Admin"
                    }
                );
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(u => u.RoleId).HasDefaultValue(2);
                entity.Property(u => u.ApprovalStatusId).HasDefaultValue(1);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");

                entity.HasOne(u => u.UserRole)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.ApprovalStatus)
                    .WithMany(s => s.Users)
                    .HasForeignKey(u => u.ApprovalStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RegistrationRequest>(entity =>
            {
                entity.Property(r => r.ApprovalStatusId).HasDefaultValue(1);
                entity.Property(r => r.SubmittedAt).HasDefaultValueSql("NOW()");

                entity.HasOne(r => r.User)
                    .WithMany(u => u.RegistrationRequests)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.ApprovalStatus)
                    .WithMany(s => s.RegistrationRequests)
                    .HasForeignKey(r => r.ApprovalStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
