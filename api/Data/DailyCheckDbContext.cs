using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace api.Data;

public partial class DailyCheckDbContext : DbContext
{
    public DailyCheckDbContext()
    {
    }

    public DailyCheckDbContext(DbContextOptions<DailyCheckDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Checkin> Checkins { get; set; }

    public virtual DbSet<CheckinPlan> CheckinPlans { get; set; }

    public virtual DbSet<SoftDeleteLog> SoftDeleteLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivity> UserActivities { get; set; }

    public virtual DbSet<UserBlacklistRecord> UserBlacklistRecords { get; set; }

    public virtual DbSet<UserOauthAccount> UserOauthAccounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=8.137.127.7;database=dailycheck;charset=utf8;uid=dailycheck;pwd=CjxCewwA7CiMk4ce;port=3306", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.36-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Checkin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("checkins", tb => tb.HasComment("打卡记录表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.IsDeleted, "idx_checkins_is_deleted");

            entity.HasIndex(e => e.Status, "idx_checkins_status");

            entity.HasIndex(e => new { e.UserId, e.CheckDate }, "idx_checkins_user_date");

            entity.HasIndex(e => new { e.PlanId, e.CheckDate, e.TimeSlotIndex }, "ux_checkins_plan_date_slot").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.CheckDate)
                .HasComment("打卡日期（仅日期）")
                .HasColumnName("check_date");
            entity.Property(e => e.TimeSlotIndex)
                .HasComment("时间段索引")
                .HasColumnName("time_slot_index");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("创建时间")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasComment("伪删除时间")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Images)
                .HasComment("打卡图片URL数组(JSON)")
                .HasColumnType("json")
                .HasColumnName("images");
            entity.Property(e => e.IsDeleted)
                .HasComment("是否伪删除：0正常，1已删除")
                .HasColumnName("is_deleted");
            entity.Property(e => e.Note)
                .HasComment("打卡备注")
                .HasColumnType("text")
                .HasColumnName("note");
            entity.Property(e => e.PlanId)
                .HasComment("所属打卡计划ID")
                .HasColumnName("plan_id");
            entity.Property(e => e.Status)
                .HasComment("打卡状态：0错过(红)、1成功(绿)、2补签(黄)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("更新时间")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("打卡用户ID")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Plan).WithMany(p => p.Checkins)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_checkins_plan");

            entity.HasOne(d => d.User).WithMany(p => p.Checkins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_checkins_user");
        });

        modelBuilder.Entity<CheckinPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("checkin_plans", tb => tb.HasComment("打卡计划表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.IsActive, "idx_plans_is_active");

            entity.HasIndex(e => e.IsDeleted, "idx_plans_is_deleted");

            entity.HasIndex(e => e.StartDate, "idx_plans_start_date");

            entity.HasIndex(e => e.UserId, "idx_plans_user");

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("创建时间")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasComment("伪删除时间")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description)
                .HasComment("打卡计划描述")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DailyTimeSlots)
                .HasComment("每日打卡时间段(JSON)")
                .HasColumnType("json")
                .HasColumnName("daily_time_slots");
            entity.Property(e => e.EndDate)
                .HasComment("计划结束日期（可选）")
                .HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("是否启用：1启用，0停用")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted)
                .HasComment("是否伪删除：0正常，1已删除")
                .HasColumnName("is_deleted");
            entity.Property(e => e.StartDate)
                .HasComment("计划开始日期")
                .HasColumnName("start_date");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasComment("打卡计划标题")
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("更新时间")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("计划所属用户ID")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.CheckinPlans)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_plans_user");
        });

        modelBuilder.Entity<SoftDeleteLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("soft_delete_logs", tb => tb.HasComment("伪删除操作日志表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.DeleterUserId, "fk_soft_delete_deleter");

            entity.HasIndex(e => e.DeletedAt, "idx_soft_delete_deleted_at");

            entity.HasIndex(e => new { e.TableName, e.RecordId }, "idx_soft_delete_table_record");

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.DeletedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("伪删除时间")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeleterUserId)
                .HasComment("执行伪删除操作的用户ID")
                .HasColumnName("deleter_user_id");
            entity.Property(e => e.Extra)
                .HasComment("扩展信息(JSON)，如数据快照等")
                .HasColumnType("json")
                .HasColumnName("extra");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasComment("伪删除原因")
                .HasColumnName("reason");
            entity.Property(e => e.RecordId)
                .HasComment("被伪删除记录的主键ID")
                .HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(64)
                .HasComment("伪删除记录所属表名")
                .HasColumnName("table_name");

            entity.HasOne(d => d.DeleterUser).WithMany(p => p.SoftDeleteLogs)
                .HasForeignKey(d => d.DeleterUserId)
                .HasConstraintName("fk_soft_delete_deleter");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("users", tb => tb.HasComment("用户表（支持伪删除与黑名单冻结）"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.FreezeOperatorId, "fk_users_freeze_operator");

            entity.HasIndex(e => e.FrozenAt, "idx_users_frozen_at");

            entity.HasIndex(e => e.IsDeleted, "idx_users_is_deleted");

            entity.HasIndex(e => e.Status, "idx_users_status");

            entity.HasIndex(e => e.Email, "ux_users_email").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.AccountUpdatedAt)
                .HasComment("username更新时间")
                .HasColumnType("datetime")
                .HasColumnName("account_updated_at");
            entity.Property(e => e.AvatarKey)
                .HasMaxLength(512)
                .HasComment("头像图片URL")
                .HasColumnName("avatar_key");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("创建时间")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasComment("伪删除时间")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasComment("用户邮箱（唯一）")
                .HasColumnName("email");
            entity.Property(e => e.FreezeOperatorId)
                .HasComment("执行冻结操作的管理员用户ID")
                .HasColumnName("freeze_operator_id");
            entity.Property(e => e.FrozenAt)
                .HasComment("账户冻结时间")
                .HasColumnType("datetime")
                .HasColumnName("frozen_at");
            entity.Property(e => e.FrozenReason)
                .HasMaxLength(255)
                .HasComment("账户冻结原因")
                .HasColumnName("frozen_reason");
            entity.Property(e => e.IsDeleted)
                .HasComment("是否伪删除：0正常，1已删除")
                .HasColumnName("is_deleted");
            entity.Property(e => e.NickName)
                .HasMaxLength(64)
                .HasComment("用户昵称")
                .HasColumnName("nick_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("密码哈希（如bcrypt）")
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'user'")
                .HasComment("用户角色：user普通用户，admin管理员")
                .HasColumnType("enum('user','admin')")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("账户状态：1正常，0冻结")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("更新时间")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserAccount)
                .HasMaxLength(64)
                .HasComment("用户账号（唯一）")
                .HasColumnName("user_account");

            entity.HasOne(d => d.FreezeOperator).WithMany(p => p.InverseFreezeOperator)
                .HasForeignKey(d => d.FreezeOperatorId)
                .HasConstraintName("fk_users_freeze_operator");
        });

        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("user_activity", tb => tb.HasComment("用户行为埋点日志表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CreatedAt, "idx_activity_created");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_activity_user_created");

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(64)
                .HasComment("操作名称/事件标识")
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("记录时间")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Ip)
                .HasMaxLength(45)
                .HasComment("IP地址")
                .HasColumnName("ip");
            entity.Property(e => e.Metadata)
                .HasComment("扩展元数据(JSON)，参数等")
                .HasColumnType("json")
                .HasColumnName("metadata");
            entity.Property(e => e.Path)
                .HasMaxLength(255)
                .HasComment("页面路径或操作路径")
                .HasColumnName("path");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(512)
                .HasComment("User-Agent信息")
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId)
                .HasComment("用户ID（匿名操作可为空）")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<UserBlacklistRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("user_blacklist_records", tb => tb.HasComment("用户黑名单记录表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.OperatorUserId, "fk_blacklist_operator");

            entity.HasIndex(e => new { e.UserId, e.OccurredAt }, "idx_blacklist_user");

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("操作发生时间")
                .HasColumnType("datetime")
                .HasColumnName("occurred_at");
            entity.Property(e => e.OperatorUserId)
                .HasComment("执行操作的管理员用户ID")
                .HasColumnName("operator_user_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasComment("拉黑或解封原因")
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasComment("状态：1拉黑，0解封（历史记录）")
                .HasColumnName("status");
            entity.Property(e => e.UserId)
                .HasComment("被拉黑的用户ID")
                .HasColumnName("user_id");

            entity.HasOne(d => d.OperatorUser).WithMany(p => p.UserBlacklistRecordOperatorUsers)
                .HasForeignKey(d => d.OperatorUserId)
                .HasConstraintName("fk_blacklist_operator");

            entity.HasOne(d => d.User).WithMany(p => p.UserBlacklistRecordUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_blacklist_user");
        });

        modelBuilder.Entity<UserOauthAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("user_oauth_accounts", tb => tb.HasComment("用户第三方登录账号绑定表"))
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.UserId, "idx_oauth_user");

            entity.HasIndex(e => new { e.Provider, e.OpenId }, "ux_oauth_provider_open").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("主键ID")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("绑定时间")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OpenId)
                .HasComment("第三方平台open_id")
                .HasColumnName("open_id");
            entity.Property(e => e.Provider)
                .HasComment("第三方登录平台类型")
                .HasColumnType("enum('wechat','google','apple')")
                .HasColumnName("provider");
            entity.Property(e => e.UnionId)
                .HasMaxLength(255)
                .HasComment("第三方平台union_id（可选）")
                .HasColumnName("union_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("更新时间")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("关联用户ID")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserOauthAccounts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_oauth_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
