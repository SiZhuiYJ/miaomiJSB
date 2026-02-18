USE dailycheck;

CREATE TABLE `users` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '用户邮箱（唯一）',
    `password_hash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '密码哈希（如bcrypt）',
    `nick_name` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '用户昵称',
    `avatar_key` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '头像图片URL',
    `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否伪删除：0正常，1已删除',
    `deleted_at` datetime DEFAULT NULL COMMENT '伪删除时间',
    `status` tinyint(1) NOT NULL DEFAULT '1' COMMENT '账户状态：1正常，0冻结',
    `role` enum('user', 'admin') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'user' COMMENT '用户角色：user普通用户，admin管理员',
    `frozen_at` datetime DEFAULT NULL COMMENT '账户冻结时间',
    `frozen_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '账户冻结原因',
    `freeze_operator_id` bigint unsigned DEFAULT NULL COMMENT '执行冻结操作的管理员用户ID',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `user_account` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '用户账号（唯一）',
    `account_updated_at` datetime DEFAULT NULL COMMENT 'username更新时间',
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_users_email` (`email`),
    UNIQUE KEY `ux_users_user_account` (`user_account`),
    KEY `idx_users_status` (`status`),
    KEY `idx_users_is_deleted` (`is_deleted`),
    KEY `idx_users_frozen_at` (`frozen_at`),
    KEY `fk_users_freeze_operator` (`freeze_operator_id`),
    CONSTRAINT `fk_users_freeze_operator` FOREIGN KEY (`freeze_operator_id`) REFERENCES `users` (`id`)
) ENGINE = InnoDB AUTO_INCREMENT = 13 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '用户表（支持伪删除与黑名单冻结）'

CREATE TABLE user_oauth_accounts (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    user_id BIGINT UNSIGNED NOT NULL COMMENT '关联用户ID',
    provider ENUM('wechat', 'google', 'apple') NOT NULL COMMENT '第三方登录平台类型',
    open_id VARCHAR(255) NOT NULL COMMENT '第三方平台open_id',
    union_id VARCHAR(255) NULL COMMENT '第三方平台union_id（可选）',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '绑定时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY ux_oauth_provider_open (provider, open_id),
    KEY idx_oauth_user (user_id),
    CONSTRAINT fk_oauth_user FOREIGN KEY (user_id) REFERENCES users (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '用户第三方登录账号绑定表';

CREATE TABLE checkin_plans (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    user_id BIGINT UNSIGNED NOT NULL COMMENT '计划所属用户ID',
    title VARCHAR(255) NOT NULL COMMENT '打卡计划标题',
    description TEXT NULL COMMENT '打卡计划描述',
    start_date DATE NOT NULL COMMENT '计划开始日期',
    end_date DATE NULL COMMENT '计划结束日期（可选）',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否启用：1启用，0停用',
    is_deleted TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否伪删除：0正常，1已删除',
    deleted_at DATETIME NULL COMMENT '伪删除时间',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    KEY idx_plans_user (user_id),
    KEY idx_plans_start_date (start_date),
    KEY idx_plans_is_active (is_active),
    KEY idx_plans_is_deleted (is_deleted),
    CONSTRAINT fk_plans_user FOREIGN KEY (user_id) REFERENCES users (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '打卡计划表';

CREATE TABLE `checkin_plans` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `user_id` bigint unsigned NOT NULL COMMENT '计划所属用户ID',
    `title` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '打卡计划标题',
    `description` text COLLATE utf8mb4_unicode_ci COMMENT '打卡计划描述',
    `start_date` date NOT NULL COMMENT '计划开始日期',
    `end_date` date DEFAULT NULL COMMENT '计划结束日期（可选）',
    `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否启用：1启用，0停用',
    `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否伪删除：0正常，1已删除',
    `deleted_at` datetime DEFAULT NULL COMMENT '伪删除时间',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    `checkin_mode` tinyint unsigned NOT NULL DEFAULT '0' COMMENT '打卡模式：0-默认模式，1-时间段打卡模式',
    PRIMARY KEY (`id`),
    KEY `idx_plans_user` (`user_id`),
    KEY `idx_plans_start_date` (`start_date`),
    KEY `idx_plans_is_active` (`is_active`),
    KEY `idx_plans_is_deleted` (`is_deleted`),
    CONSTRAINT `fk_plans_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE = InnoDB AUTO_INCREMENT = 36 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '打卡计划表'

CREATE TABLE user_activity (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    user_id BIGINT UNSIGNED NULL COMMENT '用户ID（匿名操作可为空）',
    action VARCHAR(64) NOT NULL COMMENT '操作名称/事件标识',
    path VARCHAR(255) NOT NULL COMMENT '页面路径或操作路径',
    metadata JSON NULL COMMENT '扩展元数据(JSON)，参数等',
    ip VARCHAR(45) NULL COMMENT 'IP地址',
    user_agent VARCHAR(512) NULL COMMENT 'User-Agent信息',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '记录时间',
    PRIMARY KEY (id),
    KEY idx_activity_user_created (user_id, created_at),
    KEY idx_activity_created (created_at)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '用户行为埋点日志表';

CREATE TABLE soft_delete_logs (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    table_name VARCHAR(64) NOT NULL COMMENT '伪删除记录所属表名',
    record_id BIGINT UNSIGNED NOT NULL COMMENT '被伪删除记录的主键ID',
    deleter_user_id BIGINT UNSIGNED NULL COMMENT '执行伪删除操作的用户ID',
    reason VARCHAR(255) NULL COMMENT '伪删除原因',
    deleted_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '伪删除时间',
    extra JSON NULL COMMENT '扩展信息(JSON)，如数据快照等',
    PRIMARY KEY (id),
    KEY idx_soft_delete_table_record (table_name, record_id),
    KEY idx_soft_delete_deleted_at (deleted_at),
    CONSTRAINT fk_soft_delete_deleter FOREIGN KEY (deleter_user_id) REFERENCES users (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '伪删除操作日志表';

CREATE TABLE user_blacklist_records (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    user_id BIGINT UNSIGNED NOT NULL COMMENT '被拉黑的用户ID',
    operator_user_id BIGINT UNSIGNED NULL COMMENT '执行操作的管理员用户ID',
    reason VARCHAR(255) NOT NULL COMMENT '拉黑或解封原因',
    status TINYINT(1) NOT NULL COMMENT '状态：1拉黑，0解封（历史记录）',
    occurred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '操作发生时间',
    PRIMARY KEY (id),
    KEY idx_blacklist_user (user_id, occurred_at),
    CONSTRAINT fk_blacklist_user FOREIGN KEY (user_id) REFERENCES users (id),
    CONSTRAINT fk_blacklist_operator FOREIGN KEY (operator_user_id) REFERENCES users (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '用户黑名单记录表';

CREATE TABLE `checkin_plan_time_slots` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `plan_id` BIGINT UNSIGNED NOT NULL COMMENT '所属打卡计划ID',
    `slot_name` VARCHAR(64) DEFAULT NULL COMMENT '时间段名称，如“早晨”、“下午”',
    `start_time` TIME NOT NULL COMMENT '开始时间（如 09:00:00）',
    `end_time` TIME NOT NULL COMMENT '结束时间（如 10:00:00）',
    `order_num` SMALLINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '排序序号，用于界面展示顺序',
    `is_active` TINYINT(1) NOT NULL DEFAULT '1' COMMENT '是否启用：1启用，0停用',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    KEY `idx_slots_plan` (`plan_id`),
    KEY `idx_slots_active` (`is_active`),
    CONSTRAINT `fk_slots_plan` FOREIGN KEY (`plan_id`) REFERENCES `checkin_plans` (`id`) ON DELETE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '打卡计划时间段配置表（每日重复）';