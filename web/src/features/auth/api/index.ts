
import http from "@/libs/http";
import type { ResponseData } from "@/libs/http/type";
import type { Schedule } from "@/features/schedule/types";
interface ScheduleList {
    schedule: Schedule[]
}

export const ScheduleApi = {
    // 获取课表列表
    PostList() {
        return http.post<ResponseData<ScheduleList>>("/Schedule/PostList");
    },
};

