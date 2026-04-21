-- DailyCheck incremental upgrade script
-- Goal:
-- 1. Run directly on the current dailycheck database
-- 2. Add missing columns, indexes, and tables for chat moderation and friend features
-- 3. Keep the script mostly idempotent

USE `dailycheck`;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS `sp_upgrade_dailycheck_social_20260421`;

DELIMITER $$

CREATE PROCEDURE `sp_upgrade_dailycheck_social_20260421`()
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'chat_conversations'
  ) THEN
    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'conversation_status'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `conversation_status` enum('active','disbanded','archived') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '会话状态：active=正常，disbanded=已解散，archived=已归档' AFTER `is_active`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'member_limit'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `member_limit` int unsigned NOT NULL DEFAULT '500' COMMENT '群成员上限' AFTER `conversation_status`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'last_message_at'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `last_message_at` datetime DEFAULT NULL COMMENT '最后一条消息时间' AFTER `member_limit`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'disbanded_at'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `disbanded_at` datetime DEFAULT NULL COMMENT '群聊解散时间' AFTER `last_message_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'disbanded_by_user_id'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `disbanded_by_user_id` bigint unsigned DEFAULT NULL COMMENT '解散操作者用户ID' AFTER `disbanded_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND column_name = 'disband_reason'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD COLUMN `disband_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '解散原因' AFTER `disbanded_by_user_id`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND index_name = 'idx_chat_conversations_status'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD KEY `idx_chat_conversations_status` (`conversation_status`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND index_name = 'idx_chat_conversations_last_message_at'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD KEY `idx_chat_conversations_last_message_at` (`last_message_at`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversations'
        AND index_name = 'idx_chat_conversations_disbanded_by'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD KEY `idx_chat_conversations_disbanded_by` (`disbanded_by_user_id`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.referential_constraints
      WHERE constraint_schema = DATABASE()
        AND constraint_name = 'fk_chat_conversations_disbanded_by'
    ) THEN
      ALTER TABLE `chat_conversations`
        ADD CONSTRAINT `fk_chat_conversations_disbanded_by`
        FOREIGN KEY (`disbanded_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL;
    END IF;

    UPDATE `chat_conversations`
    SET `conversation_status` = CASE
      WHEN `is_active` = 1 THEN 'active'
      ELSE 'disbanded'
    END
    WHERE `conversation_status` IS NULL
       OR (`is_active` = 0 AND `conversation_status` = 'active');

    UPDATE `chat_conversations`
    SET `member_limit` = CASE
      WHEN `conversation_type` = 'direct' THEN 2
      ELSE GREATEST(COALESCE(`member_limit`, 2), 2)
    END
    WHERE `member_limit` IS NULL
       OR `member_limit` = 0
       OR `conversation_type` = 'direct';

    IF EXISTS (
      SELECT 1
      FROM information_schema.tables
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_messages'
    ) THEN
      UPDATE `chat_conversations` c
      LEFT JOIN (
        SELECT `conversation_id`, MAX(`created_at`) AS `max_created_at`
        FROM `chat_messages`
        GROUP BY `conversation_id`
      ) m ON m.`conversation_id` = c.`id`
      SET c.`last_message_at` = COALESCE(m.`max_created_at`, c.`last_message_at`, c.`updated_at`)
      WHERE c.`last_message_at` IS NULL;
    END IF;

    UPDATE `chat_conversations`
    SET `disbanded_at` = COALESCE(`disbanded_at`, `updated_at`)
    WHERE `conversation_status` = 'disbanded'
      AND `disbanded_at` IS NULL;

    ALTER TABLE `chat_conversations`
      COMMENT = '聊天会话主表（支持群管理扩展）';
  END IF;

  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'chat_conversation_members'
  ) THEN
    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'membership_status'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `membership_status` enum('active','left','kicked') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '成员关系状态：active=在群，left=主动退出，kicked=被移除' AFTER `member_role`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'invited_at'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `invited_at` datetime DEFAULT NULL COMMENT '邀请时间' AFTER `joined_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'invited_by_user_id'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `invited_by_user_id` bigint unsigned DEFAULT NULL COMMENT '邀请人用户ID' AFTER `invited_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'removed_by_user_id'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `removed_by_user_id` bigint unsigned DEFAULT NULL COMMENT '移除操作者用户ID' AFTER `left_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'removed_reason'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `removed_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '离开或移除原因' AFTER `removed_by_user_id`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'mute_mode'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `mute_mode` enum('temporary','permanent') COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '禁言模式：temporary=限时，permanent=永久' AFTER `mute_until`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'mute_reason'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `mute_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '禁言原因' AFTER `mute_mode`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'muted_at'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `muted_at` datetime DEFAULT NULL COMMENT '开始禁言时间' AFTER `mute_reason`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND column_name = 'muted_by_user_id'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD COLUMN `muted_by_user_id` bigint unsigned DEFAULT NULL COMMENT '执行禁言的用户ID' AFTER `muted_at`;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_status'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_status` (`membership_status`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_conversation_active'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_conversation_active` (`conversation_id`,`left_at`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_user_active'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_user_active` (`user_id`,`left_at`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_invited_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_invited_by` (`invited_by_user_id`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_removed_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_removed_by` (`removed_by_user_id`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_conversation_members'
        AND index_name = 'idx_chat_members_muted_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD KEY `idx_chat_members_muted_by` (`muted_by_user_id`);
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.referential_constraints
      WHERE constraint_schema = DATABASE()
        AND constraint_name = 'fk_chat_members_invited_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD CONSTRAINT `fk_chat_members_invited_by`
        FOREIGN KEY (`invited_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.referential_constraints
      WHERE constraint_schema = DATABASE()
        AND constraint_name = 'fk_chat_members_removed_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD CONSTRAINT `fk_chat_members_removed_by`
        FOREIGN KEY (`removed_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.referential_constraints
      WHERE constraint_schema = DATABASE()
        AND constraint_name = 'fk_chat_members_muted_by'
    ) THEN
      ALTER TABLE `chat_conversation_members`
        ADD CONSTRAINT `fk_chat_members_muted_by`
        FOREIGN KEY (`muted_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL;
    END IF;

    UPDATE `chat_conversation_members`
    SET `membership_status` = CASE
      WHEN `left_at` IS NULL THEN 'active'
      ELSE 'left'
    END
    WHERE `membership_status` IS NULL
       OR (`left_at` IS NULL AND `membership_status` <> 'active')
       OR (`left_at` IS NOT NULL AND `membership_status` = 'active');

    UPDATE `chat_conversation_members`
    SET `invited_at` = COALESCE(`invited_at`, `joined_at`)
    WHERE `invited_at` IS NULL;

    UPDATE `chat_conversation_members`
    SET `mute_mode` = CASE
      WHEN `is_muted` = 1 AND `mute_until` IS NULL THEN 'permanent'
      WHEN `is_muted` = 1 AND `mute_until` IS NOT NULL THEN 'temporary'
      ELSE NULL
    END
    WHERE `is_muted` = 1
      AND (`mute_mode` IS NULL OR `mute_mode` = '');

    UPDATE `chat_conversation_members`
    SET `muted_at` = COALESCE(`muted_at`, `updated_at`, `joined_at`)
    WHERE `is_muted` = 1
      AND `muted_at` IS NULL;

    ALTER TABLE `chat_conversation_members`
      COMMENT = '会话成员关系表（支持邀请、踢出、禁言）';
  END IF;

  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'chat_messages'
  ) THEN
    IF NOT EXISTS (
      SELECT 1
      FROM information_schema.statistics
      WHERE table_schema = DATABASE()
        AND table_name = 'chat_messages'
        AND index_name = 'idx_chat_messages_conversation_seq'
    ) THEN
      ALTER TABLE `chat_messages`
        ADD KEY `idx_chat_messages_conversation_seq` (`conversation_id`,`id`);
    END IF;
  END IF;

  CREATE TABLE IF NOT EXISTS `chat_group_action_logs` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `conversation_id` bigint unsigned NOT NULL COMMENT '群聊会话ID',
    `action_type` enum('create','invite','join','kick','mute','unmute','disband','transfer_owner','set_admin','unset_admin','leave') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '群管理动作类型',
    `operator_user_id` bigint unsigned DEFAULT NULL COMMENT '操作者用户ID',
    `target_user_id` bigint unsigned DEFAULT NULL COMMENT '目标用户ID',
    `related_message_id` bigint unsigned DEFAULT NULL COMMENT '关联系统消息ID',
    `action_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '操作原因',
    `action_payload` json DEFAULT NULL COMMENT '操作扩展数据(JSON)',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    PRIMARY KEY (`id`),
    KEY `idx_chat_group_logs_conversation_created` (`conversation_id`,`created_at`),
    KEY `idx_chat_group_logs_action_type` (`action_type`),
    KEY `idx_chat_group_logs_operator` (`operator_user_id`),
    KEY `idx_chat_group_logs_target` (`target_user_id`),
    KEY `idx_chat_group_logs_message` (`related_message_id`),
    CONSTRAINT `fk_chat_group_logs_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_chat_group_logs_message` FOREIGN KEY (`related_message_id`) REFERENCES `chat_messages` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_chat_group_logs_operator` FOREIGN KEY (`operator_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_chat_group_logs_target` FOREIGN KEY (`target_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='群管理操作日志表';

  CREATE TABLE IF NOT EXISTS `user_friend_requests` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `requester_user_id` bigint unsigned NOT NULL COMMENT '申请发起人用户ID',
    `receiver_user_id` bigint unsigned NOT NULL COMMENT '申请接收人用户ID',
    `source_conversation_id` bigint unsigned DEFAULT NULL COMMENT '来源群聊ID',
    `request_message` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '好友申请附言',
    `request_source` enum('account','group','search','system') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'account' COMMENT '申请来源：account=账号，group=群聊，search=搜索，system=系统',
    `request_status` enum('pending','accepted','rejected','cancelled','expired') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending' COMMENT '申请状态：pending=待处理，accepted=已通过，rejected=已拒绝，cancelled=已取消，expired=已过期',
    `handled_by_user_id` bigint unsigned DEFAULT NULL COMMENT '处理人用户ID',
    `handled_at` datetime DEFAULT NULL COMMENT '处理时间',
    `reject_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '拒绝原因',
    `expire_at` datetime DEFAULT NULL COMMENT '过期时间',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    KEY `idx_friend_requests_requester_status` (`requester_user_id`,`request_status`,`created_at`),
    KEY `idx_friend_requests_receiver_status` (`receiver_user_id`,`request_status`,`created_at`),
    KEY `idx_friend_requests_pair_status` (`requester_user_id`,`receiver_user_id`,`request_status`),
    KEY `idx_friend_requests_status_expire` (`request_status`,`expire_at`),
    KEY `idx_friend_requests_source_conversation` (`source_conversation_id`),
    KEY `idx_friend_requests_handled_by` (`handled_by_user_id`),
    CONSTRAINT `chk_friend_requests_not_self` CHECK (`requester_user_id` <> `receiver_user_id`),
    CONSTRAINT `fk_friend_requests_handled_by` FOREIGN KEY (`handled_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friend_requests_receiver` FOREIGN KEY (`receiver_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friend_requests_requester` FOREIGN KEY (`requester_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friend_requests_source_conversation` FOREIGN KEY (`source_conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE SET NULL
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='好友申请记录表';

  CREATE TABLE IF NOT EXISTS `user_friendships` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    `user_id` bigint unsigned NOT NULL COMMENT '用户ID',
    `friend_user_id` bigint unsigned NOT NULL COMMENT '好友用户ID',
    `source_request_id` bigint unsigned DEFAULT NULL COMMENT '来源好友申请ID',
    `source_conversation_id` bigint unsigned DEFAULT NULL COMMENT '来源群聊ID',
    `status` enum('active','deleted') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '好友状态：active=有效，deleted=已删除',
    `friend_remark` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '好友备注',
    `is_starred` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否星标好友：1是，0否',
    `is_muted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否对该好友消息免打扰：1是，0否',
    `accepted_at` datetime DEFAULT NULL COMMENT '成为好友时间',
    `created_by_user_id` bigint unsigned DEFAULT NULL COMMENT '创建关系操作者用户ID',
    `deleted_at` datetime DEFAULT NULL COMMENT '删除好友时间',
    `deleted_by_user_id` bigint unsigned DEFAULT NULL COMMENT '删除好友操作者用户ID',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_friendships_user_friend` (`user_id`,`friend_user_id`),
    KEY `idx_friendships_user_status` (`user_id`,`status`),
    KEY `idx_friendships_friend_status` (`friend_user_id`,`status`),
    KEY `idx_friendships_source_request` (`source_request_id`),
    KEY `idx_friendships_source_conversation` (`source_conversation_id`),
    KEY `idx_friendships_created_by` (`created_by_user_id`),
    KEY `idx_friendships_deleted_by` (`deleted_by_user_id`),
    CONSTRAINT `chk_friendships_not_self` CHECK (`user_id` <> `friend_user_id`),
    CONSTRAINT `fk_friendships_created_by` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_deleted_by` FOREIGN KEY (`deleted_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_friend` FOREIGN KEY (`friend_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friendships_source_conversation` FOREIGN KEY (`source_conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_source_request` FOREIGN KEY (`source_request_id`) REFERENCES `user_friend_requests` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='好友关系表（按用户维度双向存储）';
END $$

DELIMITER ;

CALL `sp_upgrade_dailycheck_social_20260421`();

DROP PROCEDURE IF EXISTS `sp_upgrade_dailycheck_social_20260421`;
