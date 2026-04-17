// types/index.ts
export type ConversationType = "direct" | "group";
export type MessageType = "text" | "image" | "video" | "audio" | "file" | "system";
export type UserRole = "owner" | "admin" | "member";
export type PendingUploadStatus = "uploading" | "processing" | "failed";

export interface MessageReference {
  id: number;
  senderUserId: number;
  senderNickName?: string | null;
  messageType: MessageType;
  content?: string | null;
  extra?: string | null;
  isRecalled: boolean;
  createdAt: string;
}

export interface MessageSummary extends MessageReference {
  replyToMessageId?: number | null;
  replyToMessage?: MessageReference | null;
}

export interface FileExtra {
  fileName: string;
  fileSize: number;
  fileUrl: string;
  fileKey?: string;
  thumbnailUrl?: string;
  duration?: number;
  mimeType?: string;
  width?: number;
  height?: number;
  localPreviewUrl?: string;
  localThumbnailUrl?: string;
}

export interface PendingUpload {
  tempId: string;
  senderUserId: number;
  senderNickName?: string | null;
  messageType: Extract<MessageType, "image" | "video" | "audio" | "file">;
  createdAt: string;
  replyToMessageId?: number | null;
  replyToMessage?: MessageReference | null;
  status: PendingUploadStatus;
  progress: number;
  confirmedMessageId?: number | null;
  fileExtra: FileExtra;
}

export interface ConversationSummary {
  id: number;
  conversationType: ConversationType;
  title?: string | null;
  userAccount?: string | null;
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
  userAccount?: string | null;
  avatarKey?: string | null;
  memberRole: UserRole;
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
  extra?: FileExtra;
  replyToMessageId?: number | null;
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

export interface ReadUser {
  userId: number;
  nickName: string;
  avatarKey?: string | null;
  readAt: string;
}

export interface MessageReadStatus {
  messageId: number;
  totalRecipients: number;
  readCount: number;
  readUsers: ReadUser[];
}
