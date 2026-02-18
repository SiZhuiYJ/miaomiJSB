// @/libs/http/axios.ts
import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
  type AxiosError,
} from "axios";
import type { AuthData } from "@/libs/api/auth/type";
import http from "./index";
import { useAuthStore } from "@/stores/auth";
// 只有请求封装用的MM，方便简写
class MM {
  private instance: AxiosInstance;
  // 初始化
  constructor(config: AxiosRequestConfig) {
    // 实例化axios
    this.instance = axios.create(config);
    // 配置拦截器
    this.interceptors();
  }

  // 拦截器
  private interceptors() {
    // 请求发送之前的拦截器：携带token
    // @ts-ignore
    this.instance.interceptors.request.use(
      (config) => this.handleRequest(config),
      (error) => this.handleRequestError(error)
    );

    this.instance.interceptors.response.use(
      (response) => this.handleResponse(response),
      (error) => this.handleResponseError(error)
    );
  }
  private async handleRequest(config: InternalAxiosRequestConfig) {
    const auth = useAuthStore();
    if (auth.accessToken) {
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${auth.accessToken}`;
    }
    console.log("请求数据", config);
    return Promise.resolve(config);
  }
  private async handleRequestError(error: AxiosError) {
    console.log("请求错误", error);
    return Promise.reject(error);
  }
  private async handleResponse(response: AxiosResponse) {
    console.log("获得数据", response);
    return Promise.resolve(response);
  }
  private async handleResponseError(error: any) {
    console.log("获取错误", error);
    const auth = useAuthStore();
    const status = error.response?.status;
    const originalRequest = error.config;

    if (
      status === 401 &&
      auth.refreshToken &&
      originalRequest &&
      !(originalRequest as any)._retry
    ) {
      (originalRequest as any)._retry = true;
      try {
        const refreshResponse = await http.post<AuthData>("/mm/Auth/refresh", {
          refreshToken: auth.refreshToken,
        });
        auth.setSession(refreshResponse.data);
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${auth.accessToken}`;
        return this.instance(originalRequest);
      } catch {
        auth.clear();
      }
    }

    return Promise.reject(error);
  }
  // Get请求
  async get<T>(url: string, params?: object): Promise<AxiosResponse<T>> {
    return await this.instance.get(url, { params });
  }
  // Post请求
  async post<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    return await this.instance.post(url, data);
  }
  // Put请求
  async put<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    return await this.instance.put(url, data);
  }
  // Delete请求
  async delete<T>(url: string): Promise<AxiosResponse<T>> {
    return await this.instance.delete(url);
  }
  // 图片上传
  async upload<T>(url: string, formData?: object): Promise<AxiosResponse<T>> {
    const auth = useAuthStore();
    return await this.instance.post(url, formData, {
      headers: {
        Authorization: "Bearer " + auth.accessToken || "",
        "Content-Type": "multipart/form-data",
      },
    });
  }
  // 下载
  async download<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const auth = useAuthStore();
    return await axios.post(import.meta.env.VITE_SERVER + url, data, {
      headers: {
        Authorization: "Bearer " + auth.accessToken || "",
      },
      responseType: "blob",
    });
  }
}

export default MM; // 实例化axios
