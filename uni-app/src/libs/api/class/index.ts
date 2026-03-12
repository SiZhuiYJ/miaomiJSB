// @/libs/api/class/index.ts
import type { ResponseData } from "@/libs/http/type";
import type { ClassDto } from "@/libs/api/class/type";
import { http } from '@/libs/class/config';

export const ClassApi = {
    // 获取课程列表
    PostClassList(): Promise<ResponseData<ClassDto[]>> {
        return http.post('/Classes/PostClassesList').then(res => res.data as ResponseData<ClassDto[]>);
    },
    PostClassesByID(userID: number): Promise<ResponseData<ClassDto[]>> {
        return http.post('/Classes/PostClassesListByID', null, { params: { userId: userID } })
            .then(res => res.data as ResponseData<ClassDto[]>);
    }
};
