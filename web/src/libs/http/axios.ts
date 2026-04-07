import type {
  AxiosError,
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
import axios, { AxiosHeaders } from "axios";
import type { AuthData } from "@/features/auth/types";
import { useAuthStore } from "@/features/auth/stores";
import { notifyWarning } from "@/utils/notification";
import router from "@/routers/index";
import { httpConfig as defaultHttpConfig, type HttpConfig } from "./config";
import type { ApiResponse, HttpRequestConfig } from "./type";

/**
 * HTTP 客户端封装：
 * 1) 统一 token 注入；
 * 2) 统一响应解包与错误处理；
 * 3) 并发请求上限控制；
 * 4) 相同请求节流复用；
 * 5) 401 自动刷新 token（带并发锁）。
 */
class MM {
  private instance: AxiosInstance;

  /** 最大并发请求数。 */
  private maxConcurrent: number;

  /** 当前进行中的请求数。 */
  private currentConcurrent = 0;

  /** 请求排队队列（超过并发上限时入队）。 */
  private requestQueue: Array<{
    config: InternalAxiosRequestConfig;
    resolve: (value: AxiosResponse | PromiseLike<AxiosResponse>) => void;
    reject: (reason?: unknown) => void;
  }> = [];

  /** 是否开启相同请求复用。 */
  private throttleEnabled: boolean;

  /** 进行中的“相同请求”映射，避免短时间重复发起。 */
  private throttleMap = new Map<string, Promise<AxiosResponse>>();

  /** 节流 key 生成器。 */
  private throttleKeyGenerator: (config: InternalAxiosRequestConfig) => string;

  /**
   * token 刷新中的 Promise 锁：
   * - 有值表示正在刷新；
   * - 其他 401 请求等待该 Promise，避免并发刷新风暴。
   */
  private refreshPromise: Promise<void> | null = null;

  constructor(axiosConfig: AxiosRequestConfig, httpConfig?: Partial<HttpConfig>) {
    this.instance = axios.create(axiosConfig);
    const finalConfig = { ...defaultHttpConfig, ...httpConfig };

    this.maxConcurrent = finalConfig.maxConcurrentRequests;
    this.throttleEnabled = finalConfig.throttleEnabled;
    this.throttleKeyGenerator = finalConfig.throttleKeyGenerator!;

    this.interceptors();
  }

  /** 注册请求与响应拦截器。 */
  private interceptors() {
    this.instance.interceptors.request.use(
      (config) => this.handleRequest(config),
      (error) => this.handleRequestError(error),
    );

    this.instance.interceptors.response.use(
      (response) => this.handleResponse(response),
      (error) => this.handleResponseError(error),
    );
  }

  /**
   * 请求拦截：注入 accessToken。
   */
  private async handleRequest(config: InternalAxiosRequestConfig) {
    const auth = useAuthStore();
    if (auth.accessToken) {
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${auth.accessToken}`;
    }
    return config;
  }

  private async handleRequestError(error: AxiosError) {
    return Promise.reject(error);
  }

  /**
   * 响应拦截：默认将后端包裹结构 ApiResponse<T> 解包为 T。
   * 如需拿原始 AxiosResponse，请传 originalResponse: true。
   */
  private async handleResponse<T>(response: AxiosResponse<ApiResponse<T>>) {
    const config = response.config as HttpRequestConfig;

    if (config.originalResponse) {
      return response;
    }

    const payload = response.data;
    if (!payload || typeof payload !== "object") {
      return Promise.reject(new Error("响应格式错误"));
    }

    // 兼容后端 code 约定：0 / 200 视为成功。
    if (payload.code !== 0 && payload.code !== 200) {
      if (config.showError !== false) {
        notifyWarning(payload.message || "请求失败");
      }
      return Promise.reject(new Error(payload.message || "请求失败"));
    }

    // 保持对现有业务调用兼容：仍返回 AxiosResponse，但将 data 替换为业务 data。
    response.data = payload.data as ApiResponse<T>;
    return response;
  }

  /**
   * 响应错误拦截：
   * - 401 自动刷新 token 并重放原请求；
   * - 支持 5xx 退避重试；
   * - 其余错误透传。
   */
  private async handleResponseError(error: AxiosError) {
    const auth = useAuthStore();
    const status = error.response?.status;
    const config = error.config as HttpRequestConfig | undefined;

    if (!config) {
      return Promise.reject(error);
    }

    // 刷新接口自己失败时，直接判定登录过期，避免死循环。
    const isRefreshRequest = config.url?.includes("/mm/Auth/refresh");
    if (isRefreshRequest && status === 401) {
      this.clearAuthAndRedirect();
      return Promise.reject({ ...error, hasClearedAuth: true });
    }

    if (status === 401 && auth.refreshToken && !config._retry) {
      config._retry = true;
      try {
        await this.refreshTokenWithLock();
        config.headers = config.headers ?? {};
        config.headers.Authorization = `Bearer ${auth.accessToken}`;
        return this.instance(config);
      } catch (refreshError) {
        this.clearAuthAndRedirect();
        return Promise.reject(refreshError);
      }
    }

    // 仅针对 5xx 执行重试（指数退避）。
    const retryCount = config.retryCount ?? 0;
    config._retryCount = config._retryCount ?? 0;
    if (retryCount > 0 && config._retryCount < retryCount && (status ?? 0) >= 500) {
      config._retryCount += 1;
      await this.sleep(500 * config._retryCount);
      return this.instance(config);
    }

    return Promise.reject(error);
  }

  /**
   * 刷新 token 的并发锁实现。
   */
  private async refreshTokenWithLock() {
    if (!this.refreshPromise) {
      const auth = useAuthStore();
      this.refreshPromise = this.post<AuthData>("/mm/Auth/refresh", {
        refreshToken: auth.refreshToken,
      })
        .then((refreshResponse) => {
          auth.clear();
          auth.setSession(refreshResponse.data);
        })
        .finally(() => {
          this.refreshPromise = null;
        });
    }

    return this.refreshPromise;
  }

  /** 登录状态失效后统一处理。 */
  private clearAuthAndRedirect() {
    const auth = useAuthStore();
    auth.clear();
    notifyWarning("登录过期，请重新登录");
    router.push("/login");
  }

  private sleep(ms: number) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * 请求统一入口：
   * - 若开启节流，短时间内相同请求直接复用 Promise；
   * - 实际发送前经过并发队列控制。
   */
  private async requestWithControl<T>(config: InternalAxiosRequestConfig): Promise<AxiosResponse<T>> {
    let throttleKey: string | undefined;

    const allowDuplicate = (config as HttpRequestConfig).allowDuplicate === true;
    if (this.throttleEnabled && !allowDuplicate) {
      throttleKey = this.throttleKeyGenerator(config);
      const existingPromise = this.throttleMap.get(throttleKey);
      if (existingPromise) {
        return existingPromise as Promise<AxiosResponse<T>>;
      }
    }

    const requestPromise = this.executeRequest<T>(config);

    if (throttleKey) {
      this.throttleMap.set(throttleKey, requestPromise as Promise<AxiosResponse>);
      requestPromise
        .finally(() => {
          this.throttleMap.delete(throttleKey);
        })
        .catch(() => {
          // 仅用于清理节流映射，不吞掉业务层错误。
        });
    }

    return requestPromise;
  }

  /** 执行请求（并发槽位不足则入队等待）。 */
  private executeRequest<T>(config: InternalAxiosRequestConfig): Promise<AxiosResponse<T>> {
    return new Promise((resolve, reject) => {
      const runRequest = () => {
        this.currentConcurrent += 1;
        this.instance
          .request(config)
          .then((response) => {
            this.currentConcurrent -= 1;
            resolve(response as AxiosResponse<T>);
            this.next();
          })
          .catch((error) => {
            this.currentConcurrent -= 1;
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

  /** 从队列中取下一个请求执行。 */
  private next() {
    if (this.requestQueue.length === 0 || this.currentConcurrent >= this.maxConcurrent) {
      return;
    }

    const nextRequest = this.requestQueue.shift();
    if (!nextRequest) return;

    this.currentConcurrent += 1;
    this.instance
      .request(nextRequest.config)
      .then((response) => {
        this.currentConcurrent -= 1;
        nextRequest.resolve(response);
        this.next();
      })
      .catch((error) => {
        this.currentConcurrent -= 1;
        nextRequest.reject(error);
        this.next();
      });
  }

  /** GET 请求。 */
  async get<T>(url: string, data?: object, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "GET",
      url,
      params: data,
      headers,
    });
  }

  /** POST 请求。 */
  async post<T>(url: string, data?: object, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "POST",
      url,
      data,
      headers,
    });
  }

  /** PUT 请求。 */
  async put<T>(url: string, data?: object, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "PUT",
      url,
      data,
      headers,
    });
  }

  /** DELETE 请求。 */
  async delete<T>(url: string, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "DELETE",
      url,
      headers,
    });
  }

  /** 文件下载请求（blob）。 */
  async download<T>(url: string, data?: object, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "POST",
      url,
      data,
      responseType: "blob",
      headers,
    });
  }

  /** 获取图片二进制（arraybuffer）。 */
  async getImage<T>(url: string, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    return this.requestWithControl<T>({
      ...config,
      method: "GET",
      url,
      responseType: "arraybuffer",
      headers,
    });
  }

  /** 文件上传请求（multipart/form-data）。 */
  async upload<T>(url: string, formData?: object, config: HttpRequestConfig = {}) {
    const headers = new AxiosHeaders();
    headers.set("Content-Type", "multipart/form-data");
    return this.requestWithControl<T>({
      ...config,
      method: "POST",
      url,
      data: formData,
      headers,
    });
  }
}

export default MM;
