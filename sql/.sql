CREATE TABLE `user_friend_requests` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键',
    `requester_user_id` bigint unsigned NOT NULL COMMENT '请求者用户ID',
    `receiver_user_id` bigint unsigned NOT NULL COMMENT '接收者用户ID',
    `source_conversation_id` bigint unsigned DEFAULT NULL COMMENT '来源群组会话ID',
    `request_message` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '请求消息',
    `request_source` enum(
        'account',
        'group',
        'search',
        'system'
    ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'account' COMMENT '请求来源（account-账号/group-群组/search-搜索/system-系统）',
    `request_status` enum(
        'pending',
        'accepted',
        'rejected',
        'cancelled',
        'expired'
    ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending' COMMENT '请求状态（pending-待处理/accepted-已接受/rejected-已拒绝/cancelled-已取消/expired-已过期）',
    `handled_by_user_id` bigint unsigned DEFAULT NULL COMMENT '处理者用户ID',
    `handled_at` datetime DEFAULT NULL COMMENT '处理时间',
    `reject_reason` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '拒绝原因',
    `expire_at` datetime DEFAULT NULL COMMENT '过期时间',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    KEY `idx_friend_requests_requester_status` (
        `requester_user_id`,
        `request_status`,
        `created_at`
    ),
    KEY `idx_friend_requests_receiver_status` (
        `receiver_user_id`,
        `request_status`,
        `created_at`
    ),
    KEY `idx_friend_requests_pair_status` (
        `requester_user_id`,
        `receiver_user_id`,
        `request_status`
    ),
    KEY `idx_friend_requests_status_expire` (`request_status`, `expire_at`),
    KEY `idx_friend_requests_source_conversation` (`source_conversation_id`),
    KEY `idx_friend_requests_handled_by` (`handled_by_user_id`),
    CONSTRAINT `fk_friend_requests_handled_by` FOREIGN KEY (`handled_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friend_requests_receiver` FOREIGN KEY (`receiver_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friend_requests_requester` FOREIGN KEY (`requester_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friend_requests_source_conversation` FOREIGN KEY (`source_conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE SET NULL,
    CONSTRAINT `chk_friend_requests_not_self` CHECK (
        (
            `requester_user_id` <> `receiver_user_id`
        )
    )
) ENGINE = InnoDB AUTO_INCREMENT = 2 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '好友请求表';

CREATE TABLE `user_friendships` (
    `id` bigint unsigned NOT NULL AUTO_INCREMENT COMMENT '主键',
    `user_id` bigint unsigned NOT NULL COMMENT '用户ID',
    `friend_user_id` bigint unsigned NOT NULL COMMENT '好友用户ID',
    `source_request_id` bigint unsigned DEFAULT NULL COMMENT '来源好友请求ID',
    `source_conversation_id` bigint unsigned DEFAULT NULL COMMENT '来源群组会话ID',
    `status` enum('active', 'deleted') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'active' COMMENT '好友关系状态（active-活跃/deleted-已删除）',
    `friend_remark` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '好友备注',
    `is_starred` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否置顶（0-否/1-是）',
    `is_muted` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否静音（0-否/1-是）',
    `accepted_at` datetime DEFAULT NULL COMMENT '接受时间',
    `created_by_user_id` bigint unsigned DEFAULT NULL COMMENT '创建关系的操作者用户ID',
    `deleted_at` datetime DEFAULT NULL COMMENT '删除时间',
    `deleted_by_user_id` bigint unsigned DEFAULT NULL COMMENT '删除关系的操作者用户ID',
    `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_friendships_user_friend` (`user_id`, `friend_user_id`),
    KEY `idx_friendships_user_status` (`user_id`, `status`),
    KEY `idx_friendships_friend_status` (`friend_user_id`, `status`),
    KEY `idx_friendships_source_request` (`source_request_id`),
    KEY `idx_friendships_source_conversation` (`source_conversation_id`),
    KEY `idx_friendships_created_by` (`created_by_user_id`),
    KEY `idx_friendships_deleted_by` (`deleted_by_user_id`),
    CONSTRAINT `fk_friendships_created_by` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_deleted_by` FOREIGN KEY (`deleted_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_friend` FOREIGN KEY (`friend_user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_friendships_source_conversation` FOREIGN KEY (`source_conversation_id`) REFERENCES `chat_conversations` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_source_request` FOREIGN KEY (`source_request_id`) REFERENCES `user_friend_requests` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_friendships_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `chk_friendships_not_self` CHECK (
        (`user_id` <> `friend_user_id`)
    )
) ENGINE = InnoDB AUTO_INCREMENT = 3 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = '好友关系表（按用户方向存储）';