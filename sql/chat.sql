-- =============================================================
-- 用户聊天模块数据库脚本（MySQL 8.0+）
-- 依附于当前数据库 dailycheck，依赖 users(id)
-- 支持 2 人及以上会话（群聊/多人聊天）
-- =============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 会话主表
DROP TABLE IF EXISTS `chat_conversations`;
CREATE TABLE `chat_conversations` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '会话ID',
  `conversation_type` ENUM('direct', 'group') NOT NULL DEFAULT 'group' COMMENT '会话类型：direct=双人，group=多人/群聊',
  `title` VARCHAR(128) DEFAULT NULL COMMENT '会话标题（群聊可配置）',
  `owner_user_id` BIGINT UNSIGNED DEFAULT NULL COMMENT '群主/创建者用户ID',
  `avatar_key` VARCHAR(512) DEFAULT NULL COMMENT '会话头像',
  `is_active` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否可用：1可用，0停用',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_chat_conversations_owner` (`owner_user_id`),
  KEY `idx_chat_conversations_type` (`conversation_type`),
  KEY `idx_chat_conversations_active` (`is_active`),
  CONSTRAINT `fk_chat_conversations_owner` FOREIGN KEY (`owner_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天会话主表';

-- 会话成员表
DROP TABLE IF EXISTS `chat_conversation_members`;
CREATE TABLE `chat_conversation_members` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `conversation_id` BIGINT UNSIGNED NOT NULL COMMENT '会话ID',
  `user_id` BIGINT UNSIGNED NOT NULL COMMENT '用户ID',
  `member_role` ENUM('owner', 'admin', 'member') NOT NULL DEFAULT 'member' COMMENT '成员角色',
  `joined_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '加入时间',
  `left_at` DATETIME DEFAULT NULL COMMENT '离开时间（NULL表示仍在会话中）',
  `mute_until` DATETIME DEFAULT NULL COMMENT '禁言截至时间（NULL表示不禁言）',
  `is_pinned` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否置顶会话：1是，0否',
  `is_muted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否消息免打扰：1是，0否',
  `last_read_message_id` BIGINT UNSIGNED DEFAULT NULL COMMENT '最后已读消息ID（逻辑引用）',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_chat_members_conversation_user` (`conversation_id`, `user_id`),
  KEY `idx_chat_members_user` (`user_id`),
  KEY `idx_chat_members_conversation` (`conversation_id`),
  CONSTRAINT `fk_chat_members_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_members_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='会话成员关系表';

-- 消息表
DROP TABLE IF EXISTS `chat_messages`;
CREATE TABLE `chat_messages` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '消息ID',
  `conversation_id` BIGINT UNSIGNED NOT NULL COMMENT '会话ID',
  `sender_user_id` BIGINT UNSIGNED NOT NULL COMMENT '发送者用户ID',
  `message_type` ENUM('text', 'image', 'file', 'system') NOT NULL DEFAULT 'text' COMMENT '消息类型',
  `content` TEXT COMMENT '消息文本内容',
  `extra` JSON DEFAULT NULL COMMENT '扩展字段(JSON)，如图片/文件信息、@信息等',
  `reply_to_message_id` BIGINT UNSIGNED DEFAULT NULL COMMENT '引用回复的消息ID',
  `is_recalled` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否撤回：1是，0否',
  `recalled_at` DATETIME DEFAULT NULL COMMENT '撤回时间',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '发送时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_chat_messages_conversation_time` (`conversation_id`, `created_at`),
  KEY `idx_chat_messages_sender` (`sender_user_id`),
  KEY `idx_chat_messages_reply` (`reply_to_message_id`),
  CONSTRAINT `fk_chat_messages_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_messages_sender` FOREIGN KEY (`sender_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_messages_reply` FOREIGN KEY (`reply_to_message_id`) REFERENCES `chat_messages` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天消息表';

-- 消息已读回执表（按用户维度）
DROP TABLE IF EXISTS `chat_message_receipts`;
CREATE TABLE `chat_message_receipts` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `message_id` BIGINT UNSIGNED NOT NULL COMMENT '消息ID',
  `user_id` BIGINT UNSIGNED NOT NULL COMMENT '已读用户ID',
  `read_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '已读时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_chat_receipts_message_user` (`message_id`, `user_id`),
  KEY `idx_chat_receipts_user` (`user_id`),
  KEY `idx_chat_receipts_read_at` (`read_at`),
  CONSTRAINT `fk_chat_receipts_message` FOREIGN KEY (`message_id`) REFERENCES `chat_messages` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_receipts_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='消息已读回执表';

SET FOREIGN_KEY_CHECKS = 1;
