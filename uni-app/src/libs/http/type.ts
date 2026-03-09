// @/libs/http/type.ts
export interface ResponseData<T = any> {
    code: number;
    data: T;
    message: string;
}
