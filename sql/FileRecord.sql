-- =============================================================
-- 聊天文件元数据记录表
-- 依附于 dailycheck 数据库，依赖 users(id) 和 chat_conversations(id)
-- =============================================================

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `chat_file_records`;

CREATE TABLE `chat_file_records` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '自增主键',
    `file_key` VARCHAR(128) NOT NULL COMMENT '文件唯一标识（SHA256哈希或组合键）',
    `conversation_id` BIGINT UNSIGNED NOT NULL COMMENT '所属会话ID',
    `uploader_user_id` BIGINT UNSIGNED NOT NULL COMMENT '上传者用户ID',
    `original_filename` VARCHAR(255) DEFAULT NULL COMMENT '原始文件名（不含路径）',
    `file_size` BIGINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '文件实际存储大小（字节）',
    `content_type` VARCHAR(64) NOT NULL COMMENT 'MIME类型',
    `is_deleted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '软删除标记：0-未删除，1-已删除',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_chat_file_records_file_key` (`file_key`),
    KEY `idx_chat_file_records_conversation` (`conversation_id`),
    KEY `idx_chat_file_records_uploader` (`uploader_user_id`),
    KEY `idx_chat_file_records_deleted` (`is_deleted`),
    KEY `idx_chat_file_records_created` (`created_at`),
    CONSTRAINT `fk_chat_file_records_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_chat_file_records_uploader` FOREIGN KEY (`uploader_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '聊天文件元数据记录表，用于权限验证与文件追溯';

SET FOREIGN_KEY_CHECKS = 1;