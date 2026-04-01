import http from "@/libs/http";

export const SettingApi = {
  UploadImage(formData: FormData) {
    return http.post<{ key: string }>("/mm/Files/avatar", formData);
  },
  GetImage(url: string) {
    return http.download<Blob>(url);
  },
};
