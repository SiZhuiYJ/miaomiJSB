// composables/useErrorHandler.ts
import { ref } from "vue";

export function useErrorHandler() {
  const errorMessage = ref("");

  function setError(error: any, fallback: string) {
    const isTimeout = error?.code === "ECONNABORTED";
    const isNetworkError =
      error?.message === "Network Error" || !error?.response;

    if (isTimeout) {
      errorMessage.value = "请求超时，请检查网络后重试";
      return;
    }
    if (isNetworkError) {
      errorMessage.value = "无法连接服务器，请确认服务已启动并检查网络";
      return;
    }
    errorMessage.value =
      error?.response?.data?.message || error?.message || fallback;
  }

  function clearError() {
    errorMessage.value = "";
  }

  return { errorMessage, setError, clearError };
}
