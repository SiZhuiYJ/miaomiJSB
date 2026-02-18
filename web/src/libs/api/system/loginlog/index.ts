// @/libs/api/class/index.ts
import http from "@/libs/http";
import type {
    ResponseData
} from "@/libs/http/type";
import type {
    IpAccessLog
} from "./type";

interface AccessLog {
    ipAccessLogs: IpAccessLog[];
}

export const AccessLogApi = {

    PostIpAccessLog() {
        return http.post<ResponseData<AccessLog>>(`/Log/ipaccesslogs`);
    },
};