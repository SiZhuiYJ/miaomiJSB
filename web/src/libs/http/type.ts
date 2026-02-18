// @/libs/api/user/type.ts

// 基础响应结构
export interface ResponseData<T> {
  code: number;
  data: T;
  message: string;
  success: boolean;
}
// 扩展 axios 配置类型
declare module "axios" {
  interface AxiosRequestConfig {
    /**
     * @description 是否显示错误提示
     */
    showError?: boolean;
    /**
     * @description 是否返回原始响应（不经过拦截器处理）
     */
    originalResponse?: boolean;
  }

  interface AxiosResponse<T = any> extends ResponseData<T> {}
}
