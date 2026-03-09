// @/libs/http/index.ts
import { API_BASE_URL } from '@/config';

interface RequestOptions {
    url: string;
    method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
    data?: any;
    header?: any;
}

class Http {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    private async request<T>(options: RequestOptions): Promise<T> {
        return new Promise((resolve, reject) => {
            uni.request({
                url: this.baseUrl + options.url,
                method: options.method || 'GET',
                data: options.data,
                header: {
                    'Content-Type': 'application/json',
                    ...options.header,
                },
                success: (res) => {
                    if (res.statusCode === 200) {
                        resolve(res.data as T);
                    } else {
                        reject(new Error(`Request failed with status ${res.statusCode}`));
                    }
                },
                fail: (err) => {
                    reject(err);
                },
            });
        });
    }

    post<T>(url: string, data?: any, header?: any): Promise<T> {
        return this.request<T>({
            url,
            method: 'POST',
            data,
            header,
        });
    }

    get<T>(url: string, data?: any, header?: any): Promise<T> {
        return this.request<T>({
            url,
            method: 'GET',
            data,
            header,
        });
    }
}

const http = new Http(API_BASE_URL);

export default http;
