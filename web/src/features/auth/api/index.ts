// @/libs/api/auth/index.ts
import http from "@/libs/http";
import type {
  ActionType,
  AuthData,
  PasswordPayload,
  RegisterRecord,
} from "../types";

export const authApi = {
  async SendEmilCode(data: { email: string; actionType: ActionType }) {
    return await http.post("/mm/Auth/email-code", data);
  },
  async UpdateProfile(data: {
    nickName: string | null;
    avatarKey: string | null;
  }) {
    return await http.post<AuthData>("/mm/Auth/profile", data);
  },

  async UpdatePassword(data: PasswordPayload) {
    return await http.post("/mm/Auth/change-password", data);
  },
  async DeactivateConfirm(data: { code: string }) {
    return await http.post("/mm/Auth/deactivate", data);
  },
  async ValidateUserAccount(userAccount: string) {
    return http.post("/mm/Auth/validate-account?userAccount=" + userAccount);
  },
  async LoginWithEmail(data: { email: string; password: string }) {
    return await http.post<AuthData>("/mm/Auth/login", data);
  },
  // async LoginWithEmailCode(data: { userAccount: string; password: string }) {
  //   return await http.post<AuthData>("/mm/Auth/login-account", data);
  // },
  async LoginWithAccount(data: { userAccount: string; password: string }) {
    return await http.post<AuthData>("/mm/Auth/login-account", data);
  },
  async Register(data: RegisterRecord) {
    return await http.post<AuthData>("/mm/Auth/register", data);
  },
};
