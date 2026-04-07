USE dailycheck;

-- 双人会话主表：每条记录代表两个用户之间的一条私聊会话。
CREATE TABLE IF NOT EXISTS chat_conversations (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    user_a_id BIGINT UNSIGNED NOT NULL COMMENT '会话参与者A用户ID',
    user_b_id BIGINT UNSIGNED NOT NULL COMMENT '会话参与者B用户ID',
    -- 规范化后的用户组合，避免 (A,B) 与 (B,A) 重复建会话。
    user_low_id BIGINT UNSIGNED AS (LEAST(user_a_id, user_b_id)) STORED COMMENT '较小用户ID（生成列）',
    user_high_id BIGINT UNSIGNED AS (GREATEST(user_a_id, user_b_id)) STORED COMMENT '较大用户ID（生成列）',
    last_message_at DATETIME NULL COMMENT '最后一条消息时间，用于会话排序',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY ux_chat_pair (user_low_id, user_high_id),
    KEY idx_chat_user_a (user_a_id),
    KEY idx_chat_user_b (user_b_id),
    KEY idx_chat_last_message_at (last_message_at),
    CONSTRAINT fk_chat_conversation_user_a FOREIGN KEY (user_a_id) REFERENCES users (id),
    CONSTRAINT fk_chat_conversation_user_b FOREIGN KEY (user_b_id) REFERENCES users (id),
    CONSTRAINT ck_chat_not_self CHECK (user_a_id <> user_b_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户双人私聊会话表';

-- 私聊消息表：记录双人会话内每一条消息。
CREATE TABLE IF NOT EXISTS chat_messages (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    conversation_id BIGINT UNSIGNED NOT NULL COMMENT '所属会话ID',
    sender_user_id BIGINT UNSIGNED NOT NULL COMMENT '发送者用户ID',
    receiver_user_id BIGINT UNSIGNED NOT NULL COMMENT '接收者用户ID',
    content TEXT NOT NULL COMMENT '文本消息内容',
    msg_type ENUM('text') NOT NULL DEFAULT 'text' COMMENT '消息类型（预留扩展）',
    send_status TINYINT NOT NULL DEFAULT 1 COMMENT '发送状态：0失败，1成功，2撤回',
    is_read TINYINT(1) NOT NULL DEFAULT 0 COMMENT '接收方是否已读：0未读，1已读',
    read_at DATETIME NULL COMMENT '已读时间',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '发送时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    KEY idx_chat_messages_conversation_created (conversation_id, created_at),
    KEY idx_chat_messages_receiver_read (receiver_user_id, is_read, created_at),
    KEY idx_chat_messages_sender_created (sender_user_id, created_at),
    CONSTRAINT fk_chat_messages_conversation FOREIGN KEY (conversation_id) REFERENCES chat_conversations (id),
    CONSTRAINT fk_chat_messages_sender FOREIGN KEY (sender_user_id) REFERENCES users (id),
    CONSTRAINT fk_chat_messages_receiver FOREIGN KEY (receiver_user_id) REFERENCES users (id),
    CONSTRAINT ck_chat_message_not_self CHECK (sender_user_id <> receiver_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户双人私聊消息表';
