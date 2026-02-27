import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
  AxiosError,

} from "axios";
import axios, { AxiosHeaders } from "axios";
import type { AuthData } from "@/features/auth/types";
import { useAuthStore } from "@/features/auth/stores";
import { notifyWarning } from "@/utils/notification";
import router from "@/routers/index";
import { httpConfig as defaultHttpConfig, type HttpConfig } from "./config"; // 导入配置

class MM {
  private instance: AxiosInstance;
  // 并发控制相关
  private maxConcurrent: number;
  private currentConcurrent: number = 0;
  private requestQueue: Array<{
    config: InternalAxiosRequestConfig;
    resolve: (value: AxiosResponse | PromiseLike<AxiosResponse>) => void;
    reject: (reason?: any) => void;
  }> = [];
  // 节流相关
  private throttleEnabled: boolean;
  private throttleMap: Map<string, Promise<AxiosResponse>> = new Map();
  private throttleKeyGenerator: (config: InternalAxiosRequestConfig) => string;

  constructor(axiosConfig: AxiosRequestConfig, httpConfig?: Partial<HttpConfig>) {
    this.instance = axios.create(axiosConfig);
    // 合并配置
    const finalConfig = { ...defaultHttpConfig, ...httpConfig };
    this.maxConcurrent = finalConfig.maxConcurrentRequests;
    this.throttleEnabled = finalConfig.throttleEnabled;
    this.throttleKeyGenerator = finalConfig.throttleKeyGenerator!;

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
    console.log("请求错误", error.config);
    return Promise.reject(error);
  }
  private async handleResponse(response: AxiosResponse) {
    console.log("获得数据", response);
    return Promise.resolve(response);
  }
  private async handleResponseError(error: AxiosError) {
    console.log("获取错误", error.config);
    const auth = useAuthStore();
    const status = error.response?.status;
    const originalRequest = error.config;

    // 检查是否是刷新请求本身
    const isRefreshRequest = originalRequest?.url?.includes('/mm/Auth/refresh');
    const retry = (error.config as any)._retry;
    console.log("刷新token", retry, "isRefreshRequest:", isRefreshRequest);

    // 如果是刷新请求且返回401，直接清除认证并跳转登录
    if (isRefreshRequest && status === 401) {
      console.warn("刷新请求返回401，清除认证信息");
      auth.clear();
      notifyWarning("登录过期，请重新登录");
      router.push("/login");
      return Promise.reject({ ...error, hasClearedAuth: true });
    }

    // 正常的401处理：检查是否需要刷新token
    if (
      status === 401 &&
      auth.refreshToken &&
      originalRequest &&
      !retry
    ) {
      // 显式设置_retry属性，确保类型安全
      (error.config as any)._retry = true;

      try {
        console.log("尝试刷新token");
        const refreshResponse = await this.post<AuthData>("/mm/Auth/refresh", {
          refreshToken: auth.refreshToken,
        });
        auth.clear();
        auth.setSession(refreshResponse.data);
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${auth.accessToken}`;
        return this.instance(originalRequest);
      } catch (refreshError) {
        console.error("刷新token失败:", refreshError);
        // 刷新失败，清除认证信息并跳转到登录页
        auth.clear();
        // 如果在浏览器环境中，可以跳转到登录页面
        notifyWarning("登录过期，请重新登录");
        router.push("/login");
        return Promise.reject({ ...error, hasClearedAuth: true });
      }
    }

    return Promise.reject(error);
  }
  // 请求统一入口：处理并发和节流
  private async requestWithControl<T>(config: InternalAxiosRequestConfig): Promise<AxiosResponse<T>> {
    let throttleKey: string | undefined;
    if (this.throttleEnabled) {
      throttleKey = this.throttleKeyGenerator(config);
      const existingPromise = this.throttleMap.get(throttleKey);
      if (existingPromise) {
        return existingPromise as Promise<AxiosResponse<T>>;
      }
    }

    const requestPromise = this.executeRequest<T>(config);

    if (throttleKey) {
      this.throttleMap.set(throttleKey, requestPromise);
      requestPromise.finally(() => {
        this.throttleMap.delete(throttleKey);
      }).catch(() => { });
    }

    return requestPromise;
  }

  // 实际执行请求，考虑并发队列
  private executeRequest<T>(config: InternalAxiosRequestConfig): Promise<AxiosResponse<T>> {
    return new Promise((resolve, reject) => {
      const runRequest = () => {
        this.currentConcurrent++;
        this.instance.request(config)
          .then((response) => {
            this.currentConcurrent--;
            resolve(response as AxiosResponse<T>);
            this.next();
          })
          .catch((error) => {
            this.currentConcurrent--;
            reject(error);
            this.next();
          });
      };

      if (this.currentConcurrent < this.maxConcurrent) {
        runRequest();
      } else {
        this.requestQueue.push({ config, resolve, reject });
      }
    });
  }

  // 从队列中取出下一个请求执行
  private next() {
    if (this.requestQueue.length > 0 && this.currentConcurrent < this.maxConcurrent) {
      const nextRequest = this.requestQueue.shift()!;
      this.currentConcurrent++;
      this.instance.request(nextRequest.config)
        .then((response) => {
          this.currentConcurrent--;
          nextRequest.resolve(response);
          this.next();
        })
        .catch((error) => {
          this.currentConcurrent--;
          nextRequest.reject(error);
          this.next();
        });
    }
  }

  // axios.ts (部分修改)
  async get<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'GET',
      url,
      params: data,
      headers, // 类型完全匹配 InternalAxiosRequestConfig['headers']
    });
  }

  async post<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'POST',
      url,
      data,
      headers,
    });
  }

  async put<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'PUT',
      url,
      data,
      headers, // 类型完全匹配 InternalAxiosRequestConfig['headers']
    });
  }

  async delete<T>(url: string): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'DELETE',
      url,
      headers,
    });
  }

  async download<T>(url: string, data?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'POST',
      url,
      data,
      responseType: 'blob',
      headers,
    });
  }

  async getImage<T>(url: string): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      method: 'GET',
      url,
      responseType: 'arraybuffer',
      headers, // 类型完全匹配 InternalAxiosRequestConfig['headers']
    });
  }
  async upload<T>(url: string, formData?: object): Promise<AxiosResponse<T>> {
    const headers = new AxiosHeaders();
    headers.set('Content-Type', 'multipart/form-data');
    return this.requestWithControl<T>({
      method: 'POST',
      url,
      data: formData,
      headers, // 类型完全匹配 InternalAxiosRequestConfig['headers']
    });
  }
}

export default MM; // 实例化axios
