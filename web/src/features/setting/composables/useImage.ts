import { useAuthStore } from "@/stores"; // 根据实际路径调整
import { notifySuccess, notifyError } from "@/utils/notification"; // 根据实际路径调整
import { SettingApi } from "@/features/setting/api";
import type { Ref } from "vue";

// 假设 loading 是外部传入的响应式变量（ref）
export function uploadFile(file: File, loading: Ref<boolean>) {
  loading.value = true;

  const formData = new FormData();
  formData.append("file", file);

  const authStore = useAuthStore();

  SettingApi.UploadImage(formData)
    .then((response) => {
      loading.value = false;

      if (response.status === 200) {
        const data = response.data;
        const key = data.key;
        if (key && authStore.user?.userId) {
          authStore.updateUser({ avatarKey: key });
          notifySuccess("头像上传成功");
        } else {
          notifyError("头像上传失败: 无效的响应");
        }
      } else {
        notifyError("头像上传失败");
      }
    })
    .catch((error) => {
      loading.value = false;
      console.error("Upload error:", error);
      notifyError("头像上传失败: 网络错误");
    });
}
/**
 * 将纯 base64 字符串转换为 File 对象
 * @param base64 - base64 编码的字符串（不含 data URL 前缀）
 * @param mimeType - 文件 MIME 类型，如 'image/jpeg'
 * @param fileName - 文件名
 */
export function base64ToFile(
  base64: string,
  mimeType: string,
  fileName: string = "file",
): File {
  const binaryString = atob(base64);
  const bytes = new Uint8Array(binaryString.length);
  for (let i = 0; i < binaryString.length; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return new File([bytes], fileName, { type: mimeType });
}

/**
 * 将 data URL 格式的 base64 转换为 File 对象
 * @param dataUrl - 完整的 data URL 字符串
 * @param fileName - 文件名（可选）
 * @returns File 对象
 */
export function dataURLToFile(
  dataUrl: string,
  fileName: string = "image.png",
): File | null {
  const arr = dataUrl.split(",");
  if (arr[0]) {
    const mime = arr[0].match(/:(.*?);/)?.[1] || "image/png";
    if (arr[1]) {
      const bstr = atob(arr[1]);
      let n = bstr.length;
      const u8arr = new Uint8Array(n);
      while (n--) {
        u8arr[n] = bstr.charCodeAt(n);
      }
      return new File([u8arr], fileName, { type: mime });
    }
    return null;
  }
  return null;
}
