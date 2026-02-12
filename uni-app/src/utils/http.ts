import { API_BASE_URL } from '../config';

// Circular dependency avoidance: We inject the store instance instead of importing it directly.
let authStore: any = null;

export const setAuthStore = (store: any) => {
  authStore = store;
};

interface RequestConfig extends UniApp.RequestOptions {
  baseURL?: string;
  params?: any;
}

type HttpConfig = Omit<RequestConfig, 'url'>;

interface HttpResponse<T = any> {
  data: T;
  statusCode: number;
  header: any;
  cookies: string[];
  errMsg: string;
}

const createHttp = () => {
  const baseURL = API_BASE_URL;

  const request = async <T = any>(options: RequestConfig): Promise<HttpResponse<T>> => {
    // Ensure we use the injected store if available
    const auth = authStore; 
    
    let url = options.url;
    if (options.baseURL !== undefined) {
      // Allow overriding baseURL, but usually we prepend global baseURL
      if (!url.startsWith('http')) {
        url = (options.baseURL || baseURL) + url;
      }
    } else {
       if (!url.startsWith('http')) {
        url = baseURL + url;
      }
    }

    // Handle params
    if (options.params) {
      const query = Object.keys(options.params)
        .map((k) => `${encodeURIComponent(k)}=${encodeURIComponent(options.params[k])}`)
        .join('&');
      url += (url.includes('?') ? '&' : '?') + query;
    }

    const headers = options.header || {};
    if (auth && auth.accessToken) {
      headers['Authorization'] = `Bearer ${auth.accessToken}`;
    }

    return new Promise((resolve, reject) => {
      uni.request({
        ...options,
        url,
        header: headers,
        success: async (res) => {
            const response = res as HttpResponse<T>;
            if (response.statusCode >= 200 && response.statusCode < 300) {
                resolve(response);
            } else if (response.statusCode === 401) {
                // If it's a login/register request, 401 means invalid credentials, so we just reject
                if (options.url?.includes('/Auth/login') || options.url?.includes('/Auth/register')) {
                  reject(response);
                  return;
                }

                if (!auth) {
                   reject(response);
                   return;
                }

                // Token refresh logic
                if (auth.refreshToken) {
                    try {
                        // Avoid infinite loop if refresh fails
                        if (options.url?.includes('/Auth/refresh')) {
                             auth.clear();
                             uni.reLaunch({ url: '/pages/auth/index' }); // Redirect to login
                             reject(response);
                             return;
                        }

                        const refreshRes = await uni.request({
                            url: baseURL + '/mm/Auth/refresh',
                            method: 'POST',
                            data: { refreshToken: auth.refreshToken },
                        });

                        if (refreshRes.statusCode === 200) {
                             auth.setSession(refreshRes.data as any);
                             // Retry original request with new token
                             const retryRes = await request<T>(options);
                             resolve(retryRes);
                        } else {
                            auth.clear();
                            uni.reLaunch({ url: '/pages/auth/index' }); // Redirect to login
                            reject(response);
                        }
                    } catch (e) {
                        auth.clear();
                        uni.reLaunch({ url: '/pages/auth/index' }); // Redirect to login
                        reject(e);
                    }
                } else {
                    auth.clear();
                    uni.reLaunch({ url: '/pages/auth/index' }); // Redirect to login
                    reject(response);
                }
            } else {
                reject(response);
            }
        },
        fail: (err) => {
          reject(err);
        },
      });
    });
  };

  return {
    get: <T = any>(url: string, config?: HttpConfig) => request<T>({ ...config, url, method: 'GET' } as RequestConfig),
    post: <T = any>(url: string, data?: any, config?: HttpConfig) => request<T>({ ...config, url, method: 'POST', data } as RequestConfig),
    put: <T = any>(url: string, data?: any, config?: HttpConfig) => request<T>({ ...config, url, method: 'PUT', data } as RequestConfig),
    delete: <T = any>(url: string, config?: HttpConfig) => request<T>({ ...config, url, method: 'DELETE' } as RequestConfig),
  };
};

export const http = createHttp();
