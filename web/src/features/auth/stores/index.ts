import { defineStore } from "pinia";
import type { AuthData, AuthUser } from "@/features/auth/types";
import { CACHE_PREFIX } from "@/config";
import { authApi } from "../api";

export const useAuthStore = defineStore(
  "auth",
  () => {
    const user = ref<AuthUser | null>();
    const accessToken = ref<string | null>();
    const refreshToken = ref<string | null>();

    const isAuthenticated = computed(() => {
      return !!accessToken.value;
    });

    async function initialAuth() {
      const { data } = await authApi.getAuthData();
      user.value = data;
    }

    function setSession(payload: AuthData): void {
      user.value = {
        userId: payload.userId,
        email: payload.email,
        nickName: payload.nickName,
        userAccount: payload.userAccount,
        avatarKey: payload.avatarKey || null,
      };
      accessToken.value = payload.token;
      refreshToken.value = payload.refreshToken;
    }

    function updateUser(payload: {
      userAccount?: string | null;
      nickName?: string | null;
    }): void {
      if (user.value) {
        if (payload.userAccount !== undefined)
          user.value.userAccount = payload.userAccount;
        if (payload.nickName !== undefined)
          user.value.nickName = payload.nickName;
      }
    }

    function clear(): void {
      user.value = null;
      accessToken.value = null;
      refreshToken.value = null;
      try {
        localStorage.clear();
        sessionStorage.clear();
      } catch {}
    }

    return {
      user,
      accessToken,
      refreshToken,
      initialAuth,
      isAuthenticated,
      setSession,
      updateUser,
      clear,
    };
  },
  {
    // 持久化配置
    persist: {
      // 持久化存储的键名
      key: CACHE_PREFIX + "auth",
      // 存储方式：localStorage
      storage: localStorage,
      // 指定持久化字段
      pick: ["accessToken", "refreshToken"],
    },
  },
);
