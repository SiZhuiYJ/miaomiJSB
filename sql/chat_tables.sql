USE dailycheck;

-- 私聊会话主表（当前仅支持两人会话）
CREATE TABLE IF NOT EXISTS chat_sessions (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    session_no VARCHAR(64) NOT NULL COMMENT '会话唯一编号（业务侧生成）',
    session_type TINYINT NOT NULL DEFAULT 1 COMMENT '会话类型：1双人私聊',
    created_by_user_id BIGINT UNSIGNED NOT NULL COMMENT '创建会话的用户ID',
    last_message_id BIGINT UNSIGNED NULL COMMENT '最后一条消息ID（冗余字段，不做外键以保证脚本可重复执行）',
    last_message_at DATETIME NULL COMMENT '最后消息时间（用于会话排序）',
    is_deleted TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否伪删除：0正常，1已删除',
    deleted_at DATETIME NULL COMMENT '伪删除时间',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY ux_chat_sessions_session_no (session_no),
    KEY idx_chat_sessions_creator (created_by_user_id),
    KEY idx_chat_sessions_last_message_at (last_message_at),
    KEY idx_chat_sessions_deleted (is_deleted),
    CONSTRAINT fk_chat_sessions_creator FOREIGN KEY (created_by_user_id) REFERENCES users (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天会话表（双人私聊）';

-- 会话成员表（每个私聊会话固定2人）
CREATE TABLE IF NOT EXISTS chat_session_members (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    session_id BIGINT UNSIGNED NOT NULL COMMENT '会话ID',
    user_id BIGINT UNSIGNED NOT NULL COMMENT '成员用户ID',
    role TINYINT NOT NULL DEFAULT 1 COMMENT '成员角色：1普通成员',
    last_read_message_id BIGINT UNSIGNED NULL COMMENT '最后已读消息ID',
    last_read_at DATETIME NULL COMMENT '最后已读时间',
    unread_count INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '未读消息数（冗余字段）',
    joined_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '加入会话时间',
    is_deleted TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否伪删除：0正常，1已删除',
    deleted_at DATETIME NULL COMMENT '伪删除时间',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY ux_chat_members_session_user (session_id, user_id),
    KEY idx_chat_members_user (user_id),
    KEY idx_chat_members_session (session_id),
    KEY idx_chat_members_deleted (is_deleted),
    CONSTRAINT fk_chat_members_session FOREIGN KEY (session_id) REFERENCES chat_sessions (id),
    CONSTRAINT fk_chat_members_user FOREIGN KEY (user_id) REFERENCES users (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天会话成员表（双人私聊固定2人）';

-- 消息表（仅文本，可扩展）
CREATE TABLE IF NOT EXISTS chat_messages (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '主键ID',
    session_id BIGINT UNSIGNED NOT NULL COMMENT '会话ID',
    sender_user_id BIGINT UNSIGNED NOT NULL COMMENT '发送者用户ID',
    message_type TINYINT NOT NULL DEFAULT 1 COMMENT '消息类型：1文本',
    content TEXT NOT NULL COMMENT '消息内容',
    client_msg_no VARCHAR(64) NULL COMMENT '客户端消息号（幂等去重）',
    send_status TINYINT NOT NULL DEFAULT 1 COMMENT '发送状态：0失败，1成功，2撤回',
    is_deleted TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否伪删除：0正常，1已删除',
    deleted_at DATETIME NULL COMMENT '伪删除时间',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '发送时间',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY ux_chat_messages_session_client_no (session_id, client_msg_no),
    KEY idx_chat_messages_session_created (session_id, created_at),
    KEY idx_chat_messages_sender_created (sender_user_id, created_at),
    KEY idx_chat_messages_deleted (is_deleted),
    CONSTRAINT fk_chat_messages_session FOREIGN KEY (session_id) REFERENCES chat_sessions (id),
    CONSTRAINT fk_chat_messages_sender FOREIGN KEY (sender_user_id) REFERENCES users (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='聊天消息表';

-- 触发器：限制双人私聊每个会话最多2名成员，且同一会话成员不能重复。
DROP TRIGGER IF EXISTS trg_chat_members_before_insert;
DELIMITER $$
CREATE TRIGGER trg_chat_members_before_insert
BEFORE INSERT ON chat_session_members
FOR EACH ROW
BEGIN
    DECLARE v_member_count INT DEFAULT 0;

    SELECT COUNT(1) INTO v_member_count
    FROM chat_session_members
    WHERE session_id = NEW.session_id AND is_deleted = 0;

    IF v_member_count >= 2 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '双人私聊会话最多只能有2位成员';
    END IF;
END$$
DELIMITER ;

-- 触发器：发送消息前校验发送者必须是该会话成员。
DROP TRIGGER IF EXISTS trg_chat_messages_before_insert;
DELIMITER $$
CREATE TRIGGER trg_chat_messages_before_insert
BEFORE INSERT ON chat_messages
FOR EACH ROW
BEGIN
    DECLARE v_exists INT DEFAULT 0;

    SELECT COUNT(1) INTO v_exists
    FROM chat_session_members
    WHERE session_id = NEW.session_id
      AND user_id = NEW.sender_user_id
      AND is_deleted = 0;

    IF v_exists = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '发送者不属于当前会话';
    END IF;
END$$
DELIMITER ;
