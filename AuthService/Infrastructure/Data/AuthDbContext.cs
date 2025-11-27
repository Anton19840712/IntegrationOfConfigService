using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Data
{
        public class AuthDbContext : DbContext
        {
            public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

            public DbSet<User> Users { get; set; }
            public DbSet<Role> Roles { get; set; }
            public DbSet<Privilege> Privileges { get; set; }
            public DbSet<UserRole> UserRoles { get; set; }
            public DbSet<RolePrivilege> RolePrivileges { get; set; }
            public DbSet<RefreshToken> RefreshTokens { get; set; }
            public DbSet<AuditLog> AuditLogs { get; set; }
            public DbSet<ServiceClient> ServiceClients { get; set; }
            public DbSet<UserBehaviorProfile> UserBehaviorProfiles { get; set; }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);

                ConfigureUser(builder);
                ConfigureRole(builder);
                ConfigurePrivilege(builder);
                ConfigureUserRole(builder);
                ConfigureRolePrivilege(builder);
                ConfigureRefreshToken(builder);
                ConfigureAuditLog(builder);
                ConfigureServiceClient(builder);
                ConfigureUserBehaviorProfiles(builder);
            }

            public void ConfigureUserBehaviorProfiles(ModelBuilder builder)
            {
                builder.Entity<UserBehaviorProfile>(entity =>
                {
                    entity.HasKey(p => p.UserId);
                    builder.Entity<UserBehaviorProfile>()
                     .Property(p => p.KnownIpAddresses)
                     .HasConversion(
                         v => string.Join(',', v),
                         v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                     .Metadata.SetValueComparer(
                         new ValueComparer<List<string>>(
                             (c1, c2) => c1.SequenceEqual(c2),
                             c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                             c => c.ToList()
                         )
                     );
                    builder.Entity<UserBehaviorProfile>()
                    .Property(p => p.KnownUserAgents)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(
                        new ValueComparer<List<string>>(
                            (c1, c2) => c1.SequenceEqual(c2),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()
                        )
                    );
                    builder.Entity<UserBehaviorProfile>()
                     .Property(p => p.TypicalActiveHoursUtc)
                     .HasConversion(
                         v => string.Join(',', v),
                         v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList())
                     .Metadata.SetValueComparer(
                         new ValueComparer<List<int>>(
                             (c1, c2) => c1.SequenceEqual(c2),
                             c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                             c => c.ToList()
                         )
                     );
                    entity.HasOne(p => p.User)
                          .WithOne()
                          .HasForeignKey<UserBehaviorProfile>(p => p.UserId)
                          .OnDelete(DeleteBehavior.Cascade);
                });
            }

            private void ConfigureUser(ModelBuilder builder)
            {
                builder.Entity<User>(entity =>
                {
                    entity.HasKey(u => u.Id);
                    
                    entity.HasIndex(u => u.Login)
                        .IsUnique()
                        .HasDatabaseName("IX_Users_Login");
                        
                    entity.HasIndex(u => u.Email)
                        .IsUnique()
                        .HasDatabaseName("IX_Users_Email");
                        
                    entity.HasIndex(u => u.IsActive)
                        .HasDatabaseName("IX_Users_IsActive");

                    entity.Property(u => u.Login)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(u => u.Email)
                        .IsRequired()
                        .HasMaxLength(256);
                        
                    entity.Property(u => u.FirstName)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(u => u.LastName)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(u => u.MiddleName)
                        .HasMaxLength(100);
                        
                    entity.Property(u => u.PasswordHash)
                        .IsRequired()
                        .HasMaxLength(256);

                    entity.HasMany(u => u.RefreshTokens)
                        .WithOne()
                        .HasForeignKey(rt => rt.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                        
                    entity.HasMany(u => u.UserRoles)
                        .WithOne(ur => ur.User)
                        .HasForeignKey(ur => ur.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

            private void ConfigureRole(ModelBuilder builder)
            {
                builder.Entity<Role>(entity =>
                {
                    entity.HasKey(r => r.Id);
                    
                    entity.HasIndex(r => r.Name)
                        .IsUnique()
                        .HasDatabaseName("IX_Roles_Name");

                    entity.Property(r => r.Name)
                        .IsRequired()
                        .HasMaxLength(100);
                });
            }

            private void ConfigurePrivilege(ModelBuilder builder)
            {
                builder.Entity<Privilege>(entity =>
                {
                    entity.HasKey(p => p.Id);
                    
                    entity.HasIndex(p => p.Name)
                        .IsUnique()
                        .HasDatabaseName("IX_Privileges_Name");

                    entity.Property(p => p.Name)
                        .IsRequired()
                        .HasMaxLength(100);
                });
            }

            private void ConfigureUserRole(ModelBuilder builder)
            {
                builder.Entity<UserRole>(entity =>
                {
                    entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                    
                    entity.HasOne(ur => ur.User)
                        .WithMany(u => u.UserRoles)
                        .HasForeignKey(ur => ur.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                        
                    entity.HasOne(ur => ur.Role)
                        .WithMany(r => r.UserRoles)
                        .HasForeignKey(ur => ur.RoleId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

            private void ConfigureRolePrivilege(ModelBuilder builder)
            {
                builder.Entity<RolePrivilege>(entity =>
                {
                    entity.HasKey(rp => new { rp.RoleId, rp.PrivilegeId });
                    
                    entity.HasOne(rp => rp.Role)
                        .WithMany(r => r.RolePrivileges)
                        .HasForeignKey(rp => rp.RoleId)
                        .OnDelete(DeleteBehavior.Cascade);
                        
                    entity.HasOne(rp => rp.Privilege)
                        .WithMany(p => p.RolePrivileges)
                        .HasForeignKey(rp => rp.PrivilegeId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

            private void ConfigureRefreshToken(ModelBuilder builder)
            {
                builder.Entity<RefreshToken>(entity =>
                {
                    entity.HasKey(rt => rt.Id);
                    
                    // Критически важные индексы для производительности
                    entity.HasIndex(rt => rt.Token)
                        .IsUnique()
                        .HasDatabaseName("IX_RefreshTokens_Token");
                        
                    entity.HasIndex(rt => rt.UserId)
                        .HasDatabaseName("IX_RefreshTokens_UserId");
                        
                    entity.HasIndex(rt => rt.Expires)
                        .HasDatabaseName("IX_RefreshTokens_Expires");
                        
                    entity.HasIndex(rt => rt.CreatedAt)
                        .HasDatabaseName("IX_RefreshTokens_CreatedAt");
                        
                    entity.HasIndex(rt => rt.RevokedAt)
                        .HasDatabaseName("IX_RefreshTokens_RevokedAt");
                        
                    entity.HasIndex(rt => rt.IsUsed)
                        .HasDatabaseName("IX_RefreshTokens_IsUsed");
                        
                    entity.HasIndex(rt => rt.InvalidatedAt)
                        .HasDatabaseName("IX_RefreshTokens_InvalidatedAt");

                    entity.HasIndex(rt => rt.ReplacedByToken)
                        .HasDatabaseName("IX_RefreshTokens_ReplacedByToken");

                    entity.Property(rt => rt.Token)
                        .IsRequired()
                        .HasMaxLength(512); // Base64 токен может быть длинным
                        
                    entity.Property(rt => rt.CreatedByIp)
                        .IsRequired()
                        .HasMaxLength(45); // IPv6 максимальная длина
                        
                    entity.Property(rt => rt.RevokedByIp)
                        .HasMaxLength(45);
                        
                    entity.Property(rt => rt.ReplacedByToken)
                        .HasMaxLength(512);

                    // Значения по умолчанию для новых полей
                    entity.Property(rt => rt.IsUsed)
                        .IsRequired()
                        .HasDefaultValue(false);
                        
                    entity.HasOne<User>()
                        .WithMany(u => u.RefreshTokens)
                        .HasForeignKey(rt => rt.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

            private void ConfigureAuditLog(ModelBuilder builder)
            {
                builder.Entity<AuditLog>(entity =>
                {
                    entity.HasKey(al => al.Id);
                    
                    // Индексы для быстрого поиска и отчетов
                    entity.HasIndex(al => al.UserId)
                        .HasDatabaseName("IX_AuditLogs_UserId");
                        
                    entity.HasIndex(al => al.Timestamp)
                        .HasDatabaseName("IX_AuditLogs_Timestamp");
                        
                    entity.HasIndex(al => al.Action)
                        .HasDatabaseName("IX_AuditLogs_Action");
                        
                    entity.HasIndex(al => new { al.UserId, al.Timestamp })
                        .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

                    entity.Property(al => al.UserLogin)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(al => al.Action)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(al => al.Description)
                        .IsRequired()
                        .HasMaxLength(1024);
                        
                    entity.Property(al => al.IpAddress)
                        .IsRequired()
                        .HasMaxLength(45);
                        
                    entity.Property(al => al.AdditionalData)
                        .HasMaxLength(4000); // Для JSON данных
                });
            }

            private void ConfigureServiceClient(ModelBuilder builder)
            {
                builder.Entity<ServiceClient>(entity =>
                {
                    entity.HasKey(sc => sc.Id);
                    
                    entity.HasIndex(sc => sc.ClientId)
                        .IsUnique()
                        .HasDatabaseName("IX_ServiceClients_ClientId");
                        
                    entity.HasIndex(sc => sc.IsActive)
                        .HasDatabaseName("IX_ServiceClients_IsActive");

                    entity.Property(sc => sc.ClientId)
                        .IsRequired()
                        .HasMaxLength(100);
                        
                    entity.Property(sc => sc.ClientSecretHash)
                        .IsRequired()
                        .HasMaxLength(256);
                        
                    entity.Property(sc => sc.Name)
                        .IsRequired()
                        .HasMaxLength(100);
                });
            }
        }
    }
