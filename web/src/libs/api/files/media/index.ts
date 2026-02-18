// @/libs/api/files/index.ts
import http from "@/libs/http";
import type { ResponseData } from "@/libs/http/type";
import type { Media, tag } from "@/libs/api/files/type";
interface medias {
  medias: Media[];
}
interface Tags {
  tags: tag[];
}
let rootUrl = import.meta.env.VITE_SERVER;
export const MediaApi = {
  // 获取图片列表
  PostMediaList() {
    return http.post<ResponseData<medias>>("/files/Medias/List");
  },
  // 获取图片标签列表
  PostImageTagList() {
    return http.post<ResponseData<Tags>>("/files/Medias/tagList");
  },
  getImgLargeUrl(RainbowID: string, name: string) {
    return new URL(
      `/MeowMemoirs/Files/medias/${RainbowID}/${name.split(".")[0]}`,
      rootUrl
    ).href;
  },
  getImgMediumUrl(RainbowID: string, name: string) {
    return new URL(
      `/MeowMemoirs/Files/medias/${RainbowID}/${name.split(".")[0]}?width=200`,
      rootUrl
    ).href;
  },
  getImgSmallUrl(RainbowID: string, name: string) {
    return new URL(
      `/MeowMemoirs/Files/medias/${RainbowID}/${name.split(".")[0]}?width=200`,
      rootUrl
    ).href;
  },
  getImgOriginalUrl(RainbowID: string, name: string) {
    return new URL(
      `/MeowMemoirs/Files/medias/${RainbowID}/"${name.split(".")[0]
      }"?width=200&type=${name.split(".")[1]}`,
      rootUrl
    ).href;
  }, // 获取视频封面
  getVideoCover(RainbowID: string, name: string) {
    return new URL(
      `MeowMemoirs/Files/medias/${RainbowID}/${name.split(".")[0]
      }?MediaType=videos`,
      rootUrl
    ).href;
  },
  getVideoUrl(RainbowID: string, name: string) {
    return new URL(
      `/MeowMemoirs/Files/medias/${RainbowID}/${name.split(".")[0]
      }?type=mp4&MediaType=videos`,
      rootUrl
    ).href;
  },
};
