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