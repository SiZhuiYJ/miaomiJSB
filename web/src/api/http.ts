import axios, { AxiosError } from 'axios';
import { API_BASE_URL } from '../config';
import { useAuthStore } from '../stores/auth';

export const http = axios.create({
  baseURL: API_BASE_URL,
});

http.interceptors.request.use((config) => {
  const auth = useAuthStore();
  if (auth.accessToken) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${auth.accessToken}`;
  }
  return config;
});

const refreshClient = axios.create({
  baseURL: API_BASE_URL,
});

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const auth = useAuthStore();
    const status = error.response?.status;
    const originalRequest = error.config;

    if (status === 401 && auth.refreshToken && originalRequest && !(originalRequest as any)._retry) {
      (originalRequest as any)._retry = true;
      try {
        const refreshResponse = await refreshClient.post('/mm/Auth/refresh', {
          refreshToken: auth.refreshToken,
        });
        auth.setSession(refreshResponse.data);
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${auth.accessToken}`;
        return http(originalRequest);
      } catch {
        auth.clear();
      }
    }

    return Promise.reject(error);
  },
);
