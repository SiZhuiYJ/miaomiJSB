// @/libs/http/axios.ts
import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
  type AxiosError,
} from "axios";
import type { AuthData } from "@/features/auth/types";
import http from "./index";
import { useAuthStore } from "@/features/auth/stores";
import { useRouter } from "vue-router";
const router = useRouter();
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
      (error) => this.handleRequestError(error),
    );

    this.instance.interceptors.response.use(
      (response) => this.handleResponse(response),
      (error) => this.handleResponseError(error),
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
      } catch (refreshError) {
        console.error("刷新token失败:", refreshError);
        // 刷新失败，清除认证信息并跳转到登录页
        auth.clear();
        // 如果在浏览器环境中，可以跳转到登录页面
        router.push("/login");
        return Promise.reject({ ...error, hasClearedAuth: true });
      }
    }

    return Promise.reject(error);
  }
  // Get请求
  async get<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    return await this.instance.get(url, data);
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
