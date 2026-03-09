// @/libs/api/class/index.ts
import type { ResponseData } from "@/libs/http/type";
import type { ClassDto } from "@/libs/api/class/type";

// 课程表 API 基础 URL（单独的接口）
const CLASS_API_BASE_URL = 'https://www.meowmemoirs.cn/MeowMemoirs';

export const ClassApi = {
    // 获取课程列表
    PostClassList(): Promise<ResponseData<ClassDto[]>> {
        return new Promise((resolve, reject) => {
            uni.request({
                url: `${CLASS_API_BASE_URL}/Classes/PostClassesList`,
                method: 'POST',
                header: {
                    'Content-Type': 'application/json',
                },
                success: (res) => {
                    if (res.statusCode === 200) {
                        resolve(res.data as ResponseData<ClassDto[]>);
                    } else {
                        reject(new Error(`请求失败：${res.statusCode}`));
                    }
                },
                fail: (err) => {
                    reject(err);
                },
            });
        });
    },
    PostClassesByID(userID: number): Promise<ResponseData<ClassDto[]>> {
        return new Promise((resolve, reject) => {
            uni.request({
                url: `${CLASS_API_BASE_URL}/Classes/PostClassesListByID?userId=${userID}`,
                method: 'POST',
                header: {
                    'Content-Type': 'application/json',
                },
                success: (res) => {
                    if (res.statusCode === 200) {
                        resolve(res.data as ResponseData<ClassDto[]>);
                    } else {
                        reject(new Error(`请求失败：${res.statusCode}`));
                    }
                },
                fail: (err) => {
                    reject(err);
                },
            });
        });
    }
};
