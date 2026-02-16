using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlanDateUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_activity",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: true, comment: "用户ID（匿名操作可为空）"),
                    action = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, comment: "操作名称/事件标识", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "页面路径或操作路径", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metadata = table.Column<string>(type: "json", nullable: true, comment: "扩展元数据(JSON)，参数等", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "IP地址", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true, comment: "User-Agent信息", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "记录时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                },
                comment: "用户行为埋点日志表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    email = table.Column<string>(type: "varchar(255)", nullable: false, comment: "用户邮箱（唯一）", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "密码哈希（如bcrypt）", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nick_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, comment: "用户昵称", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    avatar_key = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true, comment: "头像图片URL", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否伪删除：0正常，1已删除"),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "伪删除时间"),
                    status = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "账户状态：1正常，0冻结"),
                    role = table.Column<string>(type: "enum('user','admin')", nullable: false, defaultValueSql: "'user'", comment: "用户角色：user普通用户，admin管理员", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    frozen_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "账户冻结时间"),
                    frozen_reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, comment: "账户冻结原因", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    freeze_operator_id = table.Column<ulong>(type: "bigint unsigned", nullable: true, comment: "执行冻结操作的管理员用户ID"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "创建时间"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "更新时间")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    user_account = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, comment: "用户账号（唯一）", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_updated_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "username更新时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_freeze_operator",
                        column: x => x.freeze_operator_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "用户表（支持伪删除与黑名单冻结）")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "checkin_plans",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "计划所属用户ID"),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "打卡计划标题", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "text", nullable: true, comment: "打卡计划描述", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "计划开始日期"),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "计划结束日期（可选）"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "是否启用：1启用，0停用"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否伪删除：0正常，1已删除"),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "伪删除时间"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "创建时间"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "更新时间")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_plans_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "打卡计划表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "soft_delete_logs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    table_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, comment: "伪删除记录所属表名", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "被伪删除记录的主键ID"),
                    deleter_user_id = table.Column<ulong>(type: "bigint unsigned", nullable: true, comment: "执行伪删除操作的用户ID"),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, comment: "伪删除原因", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "伪删除时间"),
                    extra = table.Column<string>(type: "json", nullable: true, comment: "扩展信息(JSON)，如数据快照等", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_soft_delete_deleter",
                        column: x => x.deleter_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "伪删除操作日志表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "user_blacklist_records",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "被拉黑的用户ID"),
                    operator_user_id = table.Column<ulong>(type: "bigint unsigned", nullable: true, comment: "执行操作的管理员用户ID"),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "拉黑或解封原因", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "状态：1拉黑，0解封（历史记录）"),
                    occurred_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "操作发生时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_blacklist_operator",
                        column: x => x.operator_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_blacklist_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "用户黑名单记录表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "user_oauth_accounts",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "关联用户ID"),
                    provider = table.Column<string>(type: "enum('wechat','google','apple')", nullable: false, comment: "第三方登录平台类型", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    open_id = table.Column<string>(type: "varchar(255)", nullable: false, comment: "第三方平台open_id", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    union_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, comment: "第三方平台union_id（可选）", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "绑定时间"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "更新时间")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_oauth_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "用户第三方登录账号绑定表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "checkin_plan_time_slots",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    plan_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "所属打卡计划ID"),
                    slot_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, comment: "时间段名称，如“早晨”、“下午”", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false, comment: "开始时间（如 09:00:00）"),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false, comment: "结束时间（如 10:00:00）"),
                    order_num = table.Column<ushort>(type: "smallint unsigned", nullable: false, comment: "排序序号，用于界面展示顺序"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'", comment: "是否启用：1启用，0停用"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "创建时间"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "更新时间")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_slots_plan",
                        column: x => x.plan_id,
                        principalTable: "checkin_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "打卡计划时间段配置表（每日重复）")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "checkins",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "主键ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    plan_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "所属打卡计划ID"),
                    time_slot_id = table.Column<ulong>(type: "bigint unsigned", nullable: true, comment: "关联的打卡时间段ID"),
                    user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, comment: "打卡用户ID"),
                    check_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "打卡日期（仅日期）"),
                    images = table.Column<string>(type: "json", nullable: true, comment: "打卡图片URL数组(JSON)", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "text", nullable: true, comment: "打卡备注", collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<sbyte>(type: "tinyint", nullable: false, comment: "打卡状态：0错过(红)、1成功(绿)、2补签(黄)"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否伪删除：0正常，1已删除"),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true, comment: "伪删除时间"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "创建时间"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "更新时间")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "fk_checkins_plan",
                        column: x => x.plan_id,
                        principalTable: "checkin_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_checkins_time_slot",
                        column: x => x.time_slot_id,
                        principalTable: "checkin_plan_time_slots",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_checkins_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "打卡记录表")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "idx_slots_active",
                table: "checkin_plan_time_slots",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_slots_plan",
                table: "checkin_plan_time_slots",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_plans_is_active",
                table: "checkin_plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_plans_is_deleted",
                table: "checkin_plans",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_plans_start_date",
                table: "checkin_plans",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "idx_plans_user",
                table: "checkin_plans",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "fk_checkins_time_slot",
                table: "checkins",
                column: "time_slot_id");

            migrationBuilder.CreateIndex(
                name: "idx_checkins_is_deleted",
                table: "checkins",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_checkins_status",
                table: "checkins",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_checkins_user_date",
                table: "checkins",
                columns: new[] { "user_id", "check_date" });

            migrationBuilder.CreateIndex(
                name: "ux_checkins_plan_date_slot",
                table: "checkins",
                columns: new[] { "plan_id", "check_date", "time_slot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_soft_delete_deleter",
                table: "soft_delete_logs",
                column: "deleter_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_soft_delete_deleted_at",
                table: "soft_delete_logs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "idx_soft_delete_table_record",
                table: "soft_delete_logs",
                columns: new[] { "table_name", "record_id" });

            migrationBuilder.CreateIndex(
                name: "idx_activity_created",
                table: "user_activity",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_activity_user_created",
                table: "user_activity",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "fk_blacklist_operator",
                table: "user_blacklist_records",
                column: "operator_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_blacklist_user",
                table: "user_blacklist_records",
                columns: new[] { "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_oauth_user",
                table: "user_oauth_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_oauth_provider_open",
                table: "user_oauth_accounts",
                columns: new[] { "provider", "open_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_users_freeze_operator",
                table: "users",
                column: "freeze_operator_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_frozen_at",
                table: "users",
                column: "frozen_at");

            migrationBuilder.CreateIndex(
                name: "idx_users_is_deleted",
                table: "users",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_users_status",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkins");

            migrationBuilder.DropTable(
                name: "soft_delete_logs");

            migrationBuilder.DropTable(
                name: "user_activity");

            migrationBuilder.DropTable(
                name: "user_blacklist_records");

            migrationBuilder.DropTable(
                name: "user_oauth_accounts");

            migrationBuilder.DropTable(
                name: "checkin_plan_time_slots");

            migrationBuilder.DropTable(
                name: "checkin_plans");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
