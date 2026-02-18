// @/libs/api/class/index.ts
import http from "@/libs/http";
import type {
  ResponseData
} from "@/libs/http/type";
import type {
  IPLocation,
  IPInfo
} from "./type";

interface Location {
  ipLocation: IPLocation;
}
interface SimpleIP {
  ipInfo: IPInfo;
}
export const LocationApi = {

  MMGetQueryLocation() {
    return http.get < ResponseData < Location>>(`/Access/myip`);
  },
  MMGetQueryLocationByIP(ip: string) {
    return http.get < ResponseData < Location>>(`/Access/query?ip=${ip}`);
  },
  MMGetSimpleIP() {
    return http.get < ResponseData < SimpleIP>>(`/SimpleIP/myip`);
  },
  MMGetSimpleIPByIP(ip: string) {
    return http.get < ResponseData < SimpleIP>>(`/SimpleIP/query?ip=${ip}`);
  },

  MMGetQuerySimpleIP(ip: string) {
    return http.get < ResponseData < SimpleIP>>(`/SimpleIP/${ip}`);
  },
};