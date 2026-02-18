import { defineStore } from 'pinia';
import type { AuthData, AuthState } from '@/libs/api/auth/type';

const STORAGE_KEY = 'mm_auth';

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

export const useAuthStore = defineStore('auth', () => {
  const state = loadInitialState();

  const user = ref(state.user);
  const accessToken = ref(state.accessToken);
  const refreshToken = ref(state.refreshToken);

  const isAuthenticated = computed(() => {
    return !!accessToken.value;
  });

  function setSession(payload: AuthData): void {
    user.value = {
      userId: payload.userId,
      email: payload.email,
      nickName: payload.nickName,
      userAccount: payload.userAccount,
      avatarKey: payload.avatarKey || null,
      avatarUrl: payload.avatarUrl || null,
    };
    accessToken.value = payload.token;
    refreshToken.value = payload.refreshToken;
    persist();
  }

  function updateUser(payload: {
    userAccount?: string | null;
    nickName?: string | null;
  }): void {
    if (user.value) {
      if (payload.userAccount !== undefined) user.value.userAccount = payload.userAccount;
      if (payload.nickName !== undefined) user.value.nickName = payload.nickName;
      persist();
    }
  }

  function clear(): void {
    user.value = null;
    accessToken.value = null;
    refreshToken.value = null;
    try {
      localStorage.clear();
      sessionStorage.clear();
    } catch {
    }
  }

  function persist(): void {
    const data: AuthState = {
      user: user.value,
      accessToken: accessToken.value,
      refreshToken: refreshToken.value,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }

  return {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    setSession,
    updateUser,
    clear,
    persist,
  };
});