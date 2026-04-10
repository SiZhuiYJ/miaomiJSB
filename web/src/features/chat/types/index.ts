export type ConversationType = 'direct' | 'group';
export type MessageType = 'text' | 'image' | 'file' | 'system';

export interface MessageSummary {
  id: number;
  senderUserId: number;
  senderNickName?: string | null;
  messageType: MessageType;
  content?: string | null;
  extra?: string | null;
  replyToMessageId?: number | null;
  isRecalled: boolean;
  createdAt: string;
}

export interface ConversationSummary {
  id: number;
  conversationType: ConversationType;
  title?: string | null;
  avatarKey?: string | null;
  avatarUserId?: number | null;
  isActive: boolean;
  isPinned: boolean;
  isMuted: boolean;
  updatedAt: string;
  unreadCount: number;
  lastMessage?: MessageSummary | null;
}

export interface ConversationMember {
  userId: number;
  nickName?: string | null;
  avatarKey?: string | null;
  memberRole: 'owner' | 'admin' | 'member';
  joinedAt: string;
  lastReadMessageId?: number | null;
}

export interface ConversationDetail {
  id: number;
  conversationType: ConversationType;
  title?: string | null;
  avatarKey?: string | null;
  avatarUserId?: number | null;
  isActive: boolean;
  isPinned: boolean;
  isMuted: boolean;
  ownerUserId?: number | null;
  createdAt: string;
  updatedAt: string;
  members: ConversationMember[];
}

export interface CreateConversationPayload {
  conversationType: ConversationType;
  title?: string;
  avatarKey?: string;
  memberUserIds: number[];
}

export interface SendMessagePayload {
  messageType: MessageType;
  content?: string;
  extra?: string;
  replyToMessageId?: number;
}

export interface MessageDeltaResponse {
  hasMore: boolean;
  lastMessageId: number;
  messages: MessageSummary[];
}
export interface UpdateConversation {
  title?: string | null;
  avatarKey?: string | null;
  isActive: boolean;
  isPinned: boolean;
  isMuted: boolean;
}
