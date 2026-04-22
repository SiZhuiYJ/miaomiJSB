-- DailyCheck incremental upgrade script
-- Goal:
-- 1. Add group join request support for chat groups
-- 2. Keep the script executable on existing dailycheck databases
-- 3. Add table/column comments for schema readability

USE `dailycheck`;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS `sp_upgrade_dailycheck_group_join_requests_20260422`;

DELIMITER $$

CREATE PROCEDURE `sp_upgrade_dailycheck_group_join_requests_20260422`()
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'chat_group_join_requests'
  ) THEN
    CREATE TABLE `chat_group_join_requests` (
      `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
      `conversation_id` bigint unsigned NOT NULL COMMENT '群聊会话ID',
      `requester_user_id` bigint unsigned NOT NULL COMMENT '申请人用户ID',
      `request_message` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '申请附言',
      `request_status` enum('pending','approved','rejected','expired') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending' COMMENT '申请状态：pending=待处理,approved=已通过,rejected=已拒绝,expired=已过期',
      `handled_by_user_id` bigint unsigned DEFAULT NULL COMMENT '处理人用户ID',
      `handled_at` datetime DEFAULT NULL COMMENT '处理时间',
      `reject_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '拒绝原因',
      `expire_at` datetime DEFAULT NULL COMMENT '申请过期时间',
      `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
      `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
      PRIMARY KEY (`id`),
      KEY `idx_group_join_requests_conversation_status` (`conversation_id`,`request_status`,`created_at`),
      KEY `idx_group_join_requests_requester_status` (`requester_user_id`,`request_status`,`created_at`),
      KEY `idx_group_join_requests_pair_status` (`conversation_id`,`requester_user_id`,`request_status`),
      KEY `idx_group_join_requests_status_expire` (`request_status`,`expire_at`),
      KEY `idx_group_join_requests_handled_by` (`handled_by_user_id`),
      CONSTRAINT `fk_group_join_requests_conversation` FOREIGN KEY (`conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE CASCADE,
      CONSTRAINT `fk_group_join_requests_handled_by` FOREIGN KEY (`handled_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
      CONSTRAINT `fk_group_join_requests_requester` FOREIGN KEY (`requester_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='群聊加群申请记录表';
  END IF;

  ALTER TABLE `chat_group_join_requests`
    COMMENT = '群聊加群申请记录表';
END$$

DELIMITER ;

CALL `sp_upgrade_dailycheck_group_join_requests_20260422`();
DROP PROCEDURE IF EXISTS `sp_upgrade_dailycheck_group_join_requests_20260422`;
