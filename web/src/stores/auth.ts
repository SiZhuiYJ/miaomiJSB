import { defineStore } from 'pinia';

export interface AuthUser {
  userId: number;
  email: string;
  nickName: string | null;
  userAccount: string | null;
  avatarKey: string | null;
}

export interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
}

const STORAGE_KEY = 'dailycheck_auth';

function loadInitialState(): AuthState {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return {
      user: null,
      accessToken: null,
      refreshToken: null,
    };
  }
  try {
    const parsed = JSON.parse(raw) as AuthState;
    return parsed;
  } catch {
    return {
      user: null,
      accessToken: null,
      refreshToken: null,
    };
  }
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => loadInitialState(),
  getters: {
    isAuthenticated(state): boolean {
      return !!state.accessToken;
    },
  },
  actions: {
    setSession(payload: {
      userId: number;
      email: string;
      nickName: string | null;
      userAccount: string | null;
      token: string;
      refreshToken: string;
      avatarKey?: string | null;
    }): void {
      this.user = {
        userId: payload.userId,
        email: payload.email,
        nickName: payload.nickName,
        userAccount: payload.userAccount,
        avatarKey: payload.avatarKey || null,
      };
      this.accessToken = payload.token;
      this.refreshToken = payload.refreshToken;
      this.persist();
    },
    updateUser(payload: {
      userAccount?: string | null;
      nickName?: string | null;
    }): void {
      if (this.user) {
        if (payload.userAccount !== undefined) this.user.userAccount = payload.userAccount;
        if (payload.nickName !== undefined) this.user.nickName = payload.nickName;
        this.persist();
      }
    },
    clear(): void {
      this.user = null;
      this.accessToken = null;
      this.refreshToken = null;
      try {
        localStorage.clear();
        sessionStorage.clear();
      } catch {
      }
    },
    persist(): void {
      const data: AuthState = {
        user: this.user,
        accessToken: this.accessToken,
        refreshToken: this.refreshToken,
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    },
  },
});
