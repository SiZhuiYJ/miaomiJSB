// @/libs/api/gallery/index.ts
import http from "@/libs/http";
import type { ResponseData } from "@/libs/http/type";
import type { item } from "@/libs/api/gallery/type";
interface items {
  items: item[];
  rootPath: string;
}
export const galleryApi = {
  // 获取图片列表
  MMGetImageList() {
    return http.get<ResponseData<items>>("/Gallerys/GetImageList");
  },
  // 获取图片标签列表
  PostImageTagList() {
    return http.post<ResponseData<string[]>>("/Gallerys/GetImageTagList");
  },
};
