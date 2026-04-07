// api/request.ts
import { useAuthStore } from "@/stores";
import { API_BASE_URL } from "@/config/index";
import axios, { AxiosError, AxiosHeaders } from "axios";
import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
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
  baseURL: API_BASE_URL || "/",
  timeout: 15000,
  headers: { "Content-Type": "application/json" },
});

instance.interceptors.request.use(
  (config) => handleRequest(config),
  (error) => handleRequestError(error),
);
async function handleRequest(config: InternalAxiosRequestConfig) {
  const auth = useAuthStore();
  const token = auth.accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if ((config as RequestConfig).allowDuplicate !== true) {
    removePending(config);
    addPending(config);
  }
  return config;
}
async function handleRequestError(e: AxiosError): Promise<AxiosError> {
  return Promise.reject(e);
}
instance.interceptors.response.use(
  (response) => handleResponse(response),
  (error) => handleResponseError(error),
);
async function handleResponse(response: AxiosResponse): Promise<AxiosResponse> {
  const config = response.config as RequestConfig;
  if (config.allowDuplicate !== true) removePending(config);
  const res = response.data as ApiResponse;
  if (res.code !== 0 && res.code !== 200) {
    return Promise.reject(new Error(res.message));
  }
  return Promise.resolve(res.data);
}
async function handleResponseError(error: AxiosError): Promise<AxiosError> {
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
}
class MiaoMiHttp {
  async get<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "GET",
      url,
      params: data,
      headers,
    });
  }

  async post<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "POST",
      url,
      data,
      headers,
    });
  }

  async put<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "PUT",
      url,
      data,
      headers,
    });
  }

  async delete<T>(url: string): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "DELETE",
      url,
      headers,
    });
  }

  async download<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "POST",
      url,
      data,
      responseType: "blob",
      headers,
    });
  }

  async getImage<T>(url: string): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return instance.request<T>({
      method: "GET",
      url,
      responseType: "arraybuffer",
      headers,
    });
  }
  async upload<T>(url: string, formData?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    headers.set("Content-Type", "multipart/form-data");
    return instance.request<T>({
      method: "POST",
      url,
      data: formData,
      headers,
    });
  }
}
export const http = new MiaoMiHttp();
