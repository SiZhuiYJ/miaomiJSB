// @/libs/api/class/index.ts
import http from "@/libs/http";
import type { ResponseData } from "@/libs/http/type";
import type { ClassDto } from "@/libs/api/class/type";

export const ClassApi = {
    // 获取课程列表
    PostClassList() {
        return http.post<ResponseData<ClassDto[]>>("/Classes/PostClassesList");
    },
    PostClassesByID(userID: number) {
        return http.post<ResponseData<ClassDto[]>>(`/Classes/PostClassesListByID?userId=${userID}`);
    }
};

