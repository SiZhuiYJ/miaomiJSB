-- MySQL dump 10.13  Distrib 8.0.33, for Win64 (x86_64)
--
-- Host: 8.137.127.7    Database: dailycheck
-- ------------------------------------------------------
-- Server version	8.0.36

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `checkin_plan_time_slots`
--

DROP TABLE IF EXISTS `checkin_plan_time_slots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checkin_plan_time_slots` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `plan_id` bigint unsigned NOT NULL COMMENT '所属打卡计划ID',
  `slot_name` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '时间段名称，如“早晨”、“下午”',
  `start_time` time NOT NULL COMMENT '开始时间（如 09:00:00）',
  `end_time` time NOT NULL COMMENT '结束时间（如 10:00:00）',
  `order_num` smallint unsigned NOT NULL DEFAULT '0' COMMENT '排序序号，用于界面展示顺序',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否启用：1启用，0停用',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  KEY `idx_slots_plan` (`plan_id`),
  KEY `idx_slots_active` (`is_active`),
  CONSTRAINT `fk_slots_plan` FOREIGN KEY (`plan_id`) REFERENCES `checkin_plans` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='打卡计划时间段配置表（每日重复）';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `checkin_plans`
--

DROP TABLE IF EXISTS `checkin_plans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
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
) ENGINE=InnoDB AUTO_INCREMENT=43 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='打卡计划表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `checkins`
--

DROP TABLE IF EXISTS `checkins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checkins` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `plan_id` bigint unsigned NOT NULL COMMENT '所属打卡计划ID',
  `time_slot_id` bigint unsigned DEFAULT NULL COMMENT '关联的打卡时间段ID',
  `user_id` bigint unsigned NOT NULL COMMENT '打卡用户ID',
  `check_date` date NOT NULL COMMENT '打卡日期（仅日期）',
  `images` json DEFAULT NULL COMMENT '打卡图片URL数组(JSON)',
  `note` text COLLATE utf8mb4_unicode_ci COMMENT '打卡备注',
  `status` tinyint NOT NULL COMMENT '打卡状态：0错过(红)、1成功(绿)、2补签(黄)',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否伪删除：0正常，1已删除',
  `deleted_at` datetime DEFAULT NULL COMMENT '伪删除时间',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_checkins_plan_date_slot` (`plan_id`,`check_date`,`time_slot_id`),
  KEY `idx_checkins_user_date` (`user_id`,`check_date`),
  KEY `idx_checkins_status` (`status`),
  KEY `idx_checkins_is_deleted` (`is_deleted`),
  KEY `fk_checkins_time_slot` (`time_slot_id`),
  CONSTRAINT `fk_checkins_plan` FOREIGN KEY (`plan_id`) REFERENCES `checkin_plans` (`id`),
  CONSTRAINT `fk_checkins_time_slot` FOREIGN KEY (`time_slot_id`) REFERENCES `checkin_plan_time_slots` (`id`),
  CONSTRAINT `fk_checkins_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=165 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='打卡记录表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `soft_delete_logs`
--

DROP TABLE IF EXISTS `soft_delete_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `soft_delete_logs` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `table_name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '伪删除记录所属表名',
  `record_id` bigint unsigned NOT NULL COMMENT '被伪删除记录的主键ID',
  `deleter_user_id` bigint unsigned DEFAULT NULL COMMENT '执行伪删除操作的用户ID',
  `reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '伪删除原因',
  `deleted_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '伪删除时间',
  `extra` json DEFAULT NULL COMMENT '扩展信息(JSON)，如数据快照等',
  PRIMARY KEY (`id`),
  KEY `idx_soft_delete_table_record` (`table_name`,`record_id`),
  KEY `idx_soft_delete_deleted_at` (`deleted_at`),
  KEY `fk_soft_delete_deleter` (`deleter_user_id`),
  CONSTRAINT `fk_soft_delete_deleter` FOREIGN KEY (`deleter_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='伪删除操作日志表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user_activity`
--

DROP TABLE IF EXISTS `user_activity`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_activity` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `user_id` bigint unsigned DEFAULT NULL COMMENT '用户ID（匿名操作可为空）',
  `action` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '操作名称/事件标识',
  `path` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '页面路径或操作路径',
  `metadata` json DEFAULT NULL COMMENT '扩展元数据(JSON)，参数等',
  `ip` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'IP地址',
  `user_agent` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User-Agent信息',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '记录时间',
  PRIMARY KEY (`id`),
  KEY `idx_activity_user_created` (`user_id`,`created_at`),
  KEY `idx_activity_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户行为埋点日志表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user_blacklist_records`
--

DROP TABLE IF EXISTS `user_blacklist_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_blacklist_records` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `user_id` bigint unsigned NOT NULL COMMENT '被拉黑的用户ID',
  `operator_user_id` bigint unsigned DEFAULT NULL COMMENT '执行操作的管理员用户ID',
  `reason` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '拉黑或解封原因',
  `status` tinyint(1) NOT NULL COMMENT '状态：1拉黑，0解封（历史记录）',
  `occurred_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '操作发生时间',
  PRIMARY KEY (`id`),
  KEY `idx_blacklist_user` (`user_id`,`occurred_at`),
  KEY `fk_blacklist_operator` (`operator_user_id`),
  CONSTRAINT `fk_blacklist_operator` FOREIGN KEY (`operator_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_blacklist_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户黑名单记录表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user_oauth_accounts`
--

DROP TABLE IF EXISTS `user_oauth_accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_oauth_accounts` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `user_id` bigint unsigned NOT NULL COMMENT '关联用户ID',
  `provider` enum('wechat','google','apple') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '第三方登录平台类型',
  `open_id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '第三方平台open_id',
  `union_id` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '第三方平台union_id（可选）',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '绑定时间',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_oauth_provider_open` (`provider`,`open_id`),
  UNIQUE KEY `ux_oauth_provider_union` (`provider`,`union_id`),
  KEY `idx_oauth_user` (`user_id`),
  KEY `idx_oauth_union_id` (`union_id`),
  CONSTRAINT `fk_oauth_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户第三方登录账号绑定表';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `email` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '用户邮箱（唯一）',
  `password_hash` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '密码哈希（如bcrypt）',
  `nick_name` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '用户昵称',
  `avatar_key` varchar(512) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '头像图片URL',
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否伪删除：0正常，1已删除',
  `deleted_at` datetime DEFAULT NULL COMMENT '伪删除时间',
  `status` tinyint(1) NOT NULL DEFAULT '1' COMMENT '账户状态：1正常，0冻结',
  `role` enum('user','admin') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'user' COMMENT '用户角色：user普通用户，admin管理员',
  `frozen_at` datetime DEFAULT NULL COMMENT '账户冻结时间',
  `frozen_reason` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '账户冻结原因',
  `freeze_operator_id` bigint unsigned DEFAULT NULL COMMENT '执行冻结操作的管理员用户ID',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  `user_account` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '用户账号（唯一）',
  `account_updated_at` datetime DEFAULT NULL COMMENT 'username更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_users_user_account` (`user_account`),
  UNIQUE KEY `ux_users_email` (`email`),
  KEY `idx_users_status` (`status`),
  KEY `idx_users_is_deleted` (`is_deleted`),
  KEY `idx_users_frozen_at` (`frozen_at`),
  KEY `fk_users_freeze_operator` (`freeze_operator_id`),
  CONSTRAINT `fk_users_freeze_operator` FOREIGN KEY (`freeze_operator_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户表（支持伪删除与黑名单冻结）';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping routines for database 'dailycheck'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-07 21:32:59
