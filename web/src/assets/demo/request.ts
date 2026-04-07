// api/request.ts
import axios, { AxiosError } from "axios";
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from "axios";
import type { ApiResponse, RequestConfig } from "./types";

const pendingMap = new Map<string, AbortController>();

function getRequestKey(config: AxiosRequestConfig): string {
  return [
    config.method,
    config.url,
    JSON.stringify(config.params),
    JSON.stringify(config.data),
  ].join("&");
}

function addPending(config: AxiosRequestConfig) {
  const key = getRequestKey(config);
  const controller = new AbortController();
  config.signal = controller.signal;
  pendingMap.set(key, controller);
}

function removePending(config: AxiosRequestConfig) {
  const key = getRequestKey(config);
  if (pendingMap.has(key)) {
    pendingMap.get(key)?.abort();
    pendingMap.delete(key);
  }
}
const instance: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
  timeout: 15000,
  headers: { "Content-Type": "application/json" },
});

instance.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");
    if (token) config.headers.Authorization = `Bearer ${token}`;
    if ((config as RequestConfig).allowDuplicate !== true) {
      removePending(config);
      addPending(config);
    }
    return config;
  },
  (e) => Promise.reject(e),
);

instance.interceptors.response.use(
  async (response: AxiosResponse): Promise<AxiosResponse> => {
    const config = response.config as RequestConfig;
    if (config.allowDuplicate !== true) removePending(config);
    const res = response.data as ApiResponse;
    if (res.code !== 0 && res.code !== 200) {
      return Promise.reject(new Error(res.message));
    }
    return Promise.resolve(res.data);
  },
  async (error: AxiosError) => {
    const config = error.config as RequestConfig & { _retryCount?: number };
    if (config?.allowDuplicate !== true) removePending(config);
    if (error.code === "ERR_CANCELED") return Promise.reject(error);
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      window.location.href = "/login";
      return Promise.reject(error);
    }
    config._retryCount = config?._retryCount ?? 0;
    const retryCount = config?.retryCount ?? 0;
    if (
      retryCount > 0 &&
      config._retryCount < retryCount &&
      (error.response?.status ?? 0) >= 500
    ) {
      config._retryCount++;
      await new Promise((r) => setTimeout(r, 1000 * config._retryCount!));
      return instance(config!);
    }
    return Promise.reject(error);
  },
);

export function request<T = unknown>(config: RequestConfig): Promise<T> {
  return instance.request(config) as unknown as Promise<T>;
}

export default instance;
