// api/types.ts
import type { AxiosRequestConfig } from "axios";
export interface ApiResponse<T = any> {
  code: number;
  message: string;
  data: T;
}

export interface RequestConfig extends AxiosRequestConfig {
  /** 是否显示 loading */
  showLoading?: boolean;
  /** 是否允许重复请求（默认 true 会取消重复） */
  allowDuplicate?: boolean;
  /** 重试次数 */
  retryCount?: number;
}
