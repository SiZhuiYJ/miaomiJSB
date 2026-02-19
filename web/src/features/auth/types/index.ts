export interface AuthUser {
  userId: number;
  email: string;
  nickName: string | null;
  userAccount: string | null;
  avatarKey: string | null;
  avatarUrl: string | null;
}

export interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
}
export interface AuthData {
  userId: number;
  email: string;
  nickName: string | null;
  userAccount: string | null;
  token: string;
  refreshToken: string;
  avatarKey?: string | null;
  avatarUrl?: string | null;
}
export type ActionType =
  | "register" // "注册",
  | "signup" // "注册",
  | "login" // "登录",
  | "change-password" // "修改密码",
  | "deactivate" // "注销账号",
  | "reset-password"; // "重置密码",

export interface PasswordPayload {
  newPassword: string;
  oldPassword: string | null;
  code: string | null;
}
export interface RegisterRecord {
  email: string;
  password: string;
  nickName: string | null;
  userAccount: string | null;
  code: string;
}
