using ConfigurationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationService.Data;

/// <summary>
/// Контекст базы данных для ConfigurationService
/// </summary>
public class ConfigurationDbContext : DbContext
{
    /// <summary>
    /// Конструктор с опциями
    /// </summary>
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// SIP аккаунты пользователей
    /// </summary>
    public DbSet<SipAccount> SipAccounts { get; set; } = null!;

    /// <summary>
    /// Пул доступных SIP номеров
    /// </summary>
    public DbSet<AvailableSipAccount> AvailableSipAccounts { get; set; } = null!;

    /// <summary>
    /// Ожидающие назначения SIP номеров
    /// </summary>
    public DbSet<PendingAssignment> PendingAssignments { get; set; } = null!;

    /// <summary>
    /// Конфигурация модели
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Таблица SIP аккаунтов
        modelBuilder.Entity<SipAccount>(entity =>
        {
            entity.ToTable("sip_accounts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.SipAccountName)
                .HasColumnName("sip_account_name")
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.SipPassword)
                .HasColumnName("sip_password")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(256);

            entity.Property(e => e.SipDomain)
                .HasColumnName("sip_domain")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ProxyUri)
                .HasColumnName("proxy_uri")
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Индексы
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_sip_accounts_user_id");

            entity.HasIndex(e => e.SipAccountName)
                .IsUnique()
                .HasDatabaseName("idx_sip_accounts_sip_account_name");
        });

        // Таблица доступных SIP номеров
        modelBuilder.Entity<AvailableSipAccount>(entity =>
        {
            entity.ToTable("available_sip_accounts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SipAccountName).HasColumnName("sip_account_name").IsRequired().HasMaxLength(128);
            entity.Property(e => e.SipPassword).HasColumnName("sip_password").IsRequired().HasMaxLength(256);
            entity.Property(e => e.IsAssigned).HasColumnName("is_assigned").HasDefaultValue(false);
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SipAccountName).IsUnique();
            entity.HasIndex(e => e.IsAssigned).HasDatabaseName("idx_available_sip_accounts_is_assigned");
        });

        // Таблица ожидающих назначения
        modelBuilder.Entity<PendingAssignment>(entity =>
        {
            entity.ToTable("pending_assignments");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserLogin).HasColumnName("user_login").IsRequired().HasMaxLength(128);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(50).HasDefaultValue("WaitingForAvailableAccount");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_pending_assignments_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_pending_assignments_created_at");
        });
    }
}
