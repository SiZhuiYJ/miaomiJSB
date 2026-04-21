-- DailyCheck upgraded full schema
-- Based on sql/dailycheck_2026-04-21_202909.sql
-- Added support:
-- 1. Group invite, kick, mute/unmute, and disband
-- 2. Friend requests and friendships
-- 3. Group management audit logs

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `dailycheck`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE `dailycheck`;

DROP TABLE IF EXISTS `user_friendships`;
DROP TABLE IF EXISTS `user_friend_requests`;
DROP TABLE IF EXISTS `chat_group_action_logs`;
DROP TABLE IF EXISTS `chat_message_receipts`;
DROP TABLE IF EXISTS `chat_file_records`;
DROP TABLE IF EXISTS `chat_conversation_members`;
DROP TABLE IF EXISTS `chat_messages`;
DROP TABLE IF EXISTS `chat_conversations`;
DROP TABLE IF EXISTS `user_oauth_accounts`;
DROP TABLE IF EXISTS `user_blacklist_records`;
DROP TABLE IF EXISTS `user_activity`;
DROP TABLE IF EXISTS `soft_delete_logs`;
DROP TABLE IF EXISTS `checkins`;
DROP TABLE IF EXISTS `checkin_plan_time_slots`;
DROP TABLE IF EXISTS `checkin_plans`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `__efmigrationshistory`;

CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT 'EF Core migration id',
  `ProductVersion` varchar(32) COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT 'EF Core product version',
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='EF Core migration history';

CREATE TABLE `users` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User email, unique and nullable',
  `password_hash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Password hash',
  `nick_name` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Display name',
  `avatar_key` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Avatar file key',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Soft delete flag',
  `deleted_at` datetime DEFAULT NULL COMMENT 'Soft delete time',
  `status` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Account status: 1 active, 0 frozen',
  `role` enum('user','admin') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'user' COMMENT 'User role',
  `frozen_at` datetime DEFAULT NULL COMMENT 'Freeze time',
  `frozen_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Freeze reason',
  `freeze_operator_id` bigint unsigned DEFAULT NULL COMMENT 'Admin user id who froze the account',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  `user_account` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique user account',
  `account_updated_at` datetime DEFAULT NULL COMMENT 'Account last update time',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_users_user_account` (`user_account`),
  UNIQUE KEY `ux_users_email` (`email`),
  KEY `idx_users_status` (`status`),
  KEY `idx_users_is_deleted` (`is_deleted`),
  KEY `idx_users_frozen_at` (`frozen_at`),
  KEY `fk_users_freeze_operator` (`freeze_operator_id`),
  CONSTRAINT `fk_users_freeze_operator` FOREIGN KEY (`freeze_operator_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Users';

CREATE TABLE `checkin_plans` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `user_id` bigint unsigned NOT NULL COMMENT 'Owner user id',
  `title` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Plan title',
  `description` text COLLATE utf8mb4_unicode_ci COMMENT 'Plan description',
  `start_date` date NOT NULL COMMENT 'Start date',
  `end_date` date DEFAULT NULL COMMENT 'End date',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Active flag',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Soft delete flag',
  `deleted_at` datetime DEFAULT NULL COMMENT 'Soft delete time',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  `checkin_mode` tinyint unsigned NOT NULL DEFAULT '0' COMMENT 'Mode: 0 normal, 1 slot based',
  PRIMARY KEY (`id`),
  KEY `idx_plans_user` (`user_id`),
  KEY `idx_plans_start_date` (`start_date`),
  KEY `idx_plans_is_active` (`is_active`),
  KEY `idx_plans_is_deleted` (`is_deleted`),
  CONSTRAINT `fk_plans_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Checkin plans';

CREATE TABLE `checkin_plan_time_slots` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `plan_id` bigint unsigned NOT NULL COMMENT 'Plan id',
  `slot_name` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Slot name',
  `start_time` time NOT NULL COMMENT 'Start time',
  `end_time` time NOT NULL COMMENT 'End time',
  `order_num` smallint unsigned NOT NULL DEFAULT '0' COMMENT 'Display order',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Active flag',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  KEY `idx_slots_plan` (`plan_id`),
  KEY `idx_slots_active` (`is_active`),
  CONSTRAINT `fk_slots_plan` FOREIGN KEY (`plan_id`) REFERENCES `checkin_plans` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Checkin plan time slots';

CREATE TABLE `checkins` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `plan_id` bigint unsigned NOT NULL COMMENT 'Plan id',
  `time_slot_id` bigint unsigned DEFAULT NULL COMMENT 'Time slot id',
  `user_id` bigint unsigned NOT NULL COMMENT 'User id',
  `check_date` date NOT NULL COMMENT 'Checkin date',
  `images` json DEFAULT NULL COMMENT 'Image url array in JSON',
  `note` text COLLATE utf8mb4_unicode_ci COMMENT 'Checkin note',
  `status` tinyint NOT NULL COMMENT 'Status: 0 missed, 1 success, 2 backfill',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Soft delete flag',
  `deleted_at` datetime DEFAULT NULL COMMENT 'Soft delete time',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_checkins_plan_date_slot` (`plan_id`,`check_date`,`time_slot_id`),
  KEY `idx_checkins_user_date` (`user_id`,`check_date`),
  KEY `idx_checkins_status` (`status`),
  KEY `idx_checkins_is_deleted` (`is_deleted`),
  KEY `fk_checkins_time_slot` (`time_slot_id`),
  CONSTRAINT `fk_checkins_plan` FOREIGN KEY (`plan_id`) REFERENCES `checkin_plans` (`id`),
  CONSTRAINT `fk_checkins_time_slot` FOREIGN KEY (`time_slot_id`) REFERENCES `checkin_plan_time_slots` (`id`),
  CONSTRAINT `fk_checkins_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Checkin records';

CREATE TABLE `soft_delete_logs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `table_name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Deleted table name',
  `record_id` bigint unsigned NOT NULL COMMENT 'Deleted record id',
  `deleter_user_id` bigint unsigned DEFAULT NULL COMMENT 'Operator user id',
  `reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Delete reason',
  `deleted_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Delete time',
  `extra` json DEFAULT NULL COMMENT 'Extra JSON payload',
  PRIMARY KEY (`id`),
  KEY `idx_soft_delete_table_record` (`table_name`,`record_id`),
  KEY `idx_soft_delete_deleted_at` (`deleted_at`),
  KEY `fk_soft_delete_deleter` (`deleter_user_id`),
  CONSTRAINT `fk_soft_delete_deleter` FOREIGN KEY (`deleter_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Soft delete logs';

CREATE TABLE `user_activity` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `user_id` bigint unsigned DEFAULT NULL COMMENT 'User id, nullable',
  `action` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Action code',
  `path` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Request path or route',
  `metadata` json DEFAULT NULL COMMENT 'Extra metadata JSON',
  `ip` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Client IP',
  `user_agent` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User agent',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  PRIMARY KEY (`id`),
  KEY `idx_activity_user_created` (`user_id`,`created_at`),
  KEY `idx_activity_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User activity logs';

CREATE TABLE `user_blacklist_records` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `user_id` bigint unsigned NOT NULL COMMENT 'Target user id',
  `operator_user_id` bigint unsigned DEFAULT NULL COMMENT 'Admin operator user id',
  `reason` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Reason',
  `status` tinyint(1) NOT NULL COMMENT '1 blacklisted, 0 unblocked',
  `occurred_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Action time',
  PRIMARY KEY (`id`),
  KEY `idx_blacklist_user` (`user_id`,`occurred_at`),
  KEY `fk_blacklist_operator` (`operator_user_id`),
  CONSTRAINT `fk_blacklist_operator` FOREIGN KEY (`operator_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_blacklist_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Blacklist history';

CREATE TABLE `user_oauth_accounts` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `user_id` bigint unsigned NOT NULL COMMENT 'Related user id',
  `provider` enum('wechat','google','apple') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'OAuth provider',
  `open_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Provider open id',
  `union_id` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Provider union id',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_oauth_provider_open` (`provider`,`open_id`),
  UNIQUE KEY `ux_oauth_provider_union` (`provider`,`union_id`),
  KEY `idx_oauth_user` (`user_id`),
  KEY `idx_oauth_union_id` (`union_id`),
  CONSTRAINT `fk_oauth_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='OAuth account bindings';

CREATE TABLE `chat_conversations` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Conversation id',
  `conversation_type` enum('direct','group') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'group' COMMENT 'direct one to one, group many users',
  `title` varchar(128) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Conversation title',
  `owner_user_id` bigint unsigned DEFAULT NULL COMMENT 'Group owner user id',
  `avatar_key` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Conversation avatar key',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Active flag',
  `conversation_status` enum('active','disbanded','archived') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '会话状态：active=正常，disbanded=已解散，archived=已归档',
  `member_limit` int unsigned NOT NULL DEFAULT '500' COMMENT '群成员上限',
  `last_message_at` datetime DEFAULT NULL COMMENT '最后一条消息时间',
  `disbanded_at` datetime DEFAULT NULL COMMENT '群聊解散时间',
  `disbanded_by_user_id` bigint unsigned DEFAULT NULL COMMENT '解散操作者用户ID',
  `disband_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '解散原因',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  KEY `idx_chat_conversations_owner` (`owner_user_id`),
  KEY `idx_chat_conversations_type` (`conversation_type`),
  KEY `idx_chat_conversations_active` (`is_active`),
  KEY `idx_chat_conversations_status` (`conversation_status`),
  KEY `idx_chat_conversations_last_message_at` (`last_message_at`),
  KEY `idx_chat_conversations_disbanded_by` (`disbanded_by_user_id`),
  CONSTRAINT `fk_chat_conversations_disbanded_by` FOREIGN KEY (`disbanded_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_chat_conversations_owner` FOREIGN KEY (`owner_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天会话主表（支持群管理扩展）';

CREATE TABLE `chat_messages` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Message id',
  `conversation_id` bigint unsigned NOT NULL COMMENT 'Conversation id',
  `sender_user_id` bigint unsigned NOT NULL COMMENT 'Sender user id',
  `message_type` enum('text','image','video','audio','file','system') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'text' COMMENT 'Message type',
  `content` text COLLATE utf8mb4_unicode_ci COMMENT 'Message content',
  `extra` json DEFAULT NULL COMMENT 'Extra JSON payload',
  `reply_to_message_id` bigint unsigned DEFAULT NULL COMMENT 'Reply target message id',
  `is_recalled` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Recall flag',
  `recalled_at` datetime DEFAULT NULL COMMENT 'Recall time',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  KEY `idx_chat_messages_conversation_time` (`conversation_id`,`created_at`),
  KEY `idx_chat_messages_conversation_seq` (`conversation_id`,`id`),
  KEY `idx_chat_messages_sender` (`sender_user_id`),
  KEY `idx_chat_messages_reply` (`reply_to_message_id`),
  CONSTRAINT `fk_chat_messages_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_messages_reply` FOREIGN KEY (`reply_to_message_id`) REFERENCES `chat_messages` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_chat_messages_sender` FOREIGN KEY (`sender_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chat messages';

CREATE TABLE `chat_conversation_members` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `conversation_id` bigint unsigned NOT NULL COMMENT 'Conversation id',
  `user_id` bigint unsigned NOT NULL COMMENT 'User id',
  `member_role` enum('owner','admin','member') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'member' COMMENT 'Member role',
  `membership_status` enum('active','left','kicked') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '成员关系状态：active=在群，left=主动退出，kicked=被移除',
  `joined_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '加入时间',
  `invited_at` datetime DEFAULT NULL COMMENT '邀请时间',
  `invited_by_user_id` bigint unsigned DEFAULT NULL COMMENT '邀请人用户ID',
  `left_at` datetime DEFAULT NULL COMMENT '离开或移除时间',
  `removed_by_user_id` bigint unsigned DEFAULT NULL COMMENT '移除操作者用户ID',
  `removed_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '离开或移除原因',
  `mute_until` datetime DEFAULT NULL COMMENT '禁言截止时间',
  `mute_mode` enum('temporary','permanent') COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '禁言模式：temporary=限时，permanent=永久',
  `mute_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '禁言原因',
  `muted_at` datetime DEFAULT NULL COMMENT '开始禁言时间',
  `muted_by_user_id` bigint unsigned DEFAULT NULL COMMENT '执行禁言的用户ID',
  `is_pinned` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Pinned flag',
  `is_muted` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Conversation mute flag',
  `last_read_message_id` bigint unsigned DEFAULT NULL COMMENT 'Last read message id, logical reference only',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_chat_members_conversation_user` (`conversation_id`,`user_id`),
  KEY `idx_chat_members_user` (`user_id`),
  KEY `idx_chat_members_conversation` (`conversation_id`),
  KEY `idx_chat_members_status` (`membership_status`),
  KEY `idx_chat_members_conversation_active` (`conversation_id`,`left_at`),
  KEY `idx_chat_members_user_active` (`user_id`,`left_at`),
  KEY `idx_chat_members_invited_by` (`invited_by_user_id`),
  KEY `idx_chat_members_removed_by` (`removed_by_user_id`),
  KEY `idx_chat_members_muted_by` (`muted_by_user_id`),
  CONSTRAINT `fk_chat_members_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_members_invited_by` FOREIGN KEY (`invited_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_chat_members_muted_by` FOREIGN KEY (`muted_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_chat_members_removed_by` FOREIGN KEY (`removed_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_chat_members_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='会话成员关系表（支持邀请、踢出、禁言）';

CREATE TABLE `chat_message_receipts` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `message_id` bigint unsigned NOT NULL COMMENT 'Message id',
  `user_id` bigint unsigned NOT NULL COMMENT 'Reader user id',
  `read_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Read time',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_chat_receipts_message_user` (`message_id`,`user_id`),
  KEY `idx_chat_receipts_user` (`user_id`),
  KEY `idx_chat_receipts_read_at` (`read_at`),
  CONSTRAINT `fk_chat_receipts_message` FOREIGN KEY (`message_id`) REFERENCES `chat_messages` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_receipts_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Message read receipts';

CREATE TABLE `chat_file_records` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `file_key` varchar(128) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique file key',
  `conversation_id` bigint unsigned NOT NULL COMMENT 'Conversation id',
  `uploader_user_id` bigint unsigned NOT NULL COMMENT 'Uploader user id',
  `original_filename` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Original file name',
  `file_size` bigint unsigned NOT NULL DEFAULT '0' COMMENT 'File size in bytes',
  `content_type` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'MIME type',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Soft delete flag',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Created time',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Updated time',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_chat_file_records_file_key` (`file_key`),
  KEY `idx_chat_file_records_conversation` (`conversation_id`),
  KEY `idx_chat_file_records_uploader` (`uploader_user_id`),
  KEY `idx_chat_file_records_deleted` (`is_deleted`),
  KEY `idx_chat_file_records_created` (`created_at`),
  CONSTRAINT `fk_chat_file_records_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_file_records_uploader` FOREIGN KEY (`uploader_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chat file metadata';

CREATE TABLE `chat_group_action_logs` (
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

CREATE TABLE `user_friend_requests` (
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

CREATE TABLE `user_friendships` (
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

SET FOREIGN_KEY_CHECKS = 1;
