// @/libs/http/axios.ts
import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios";
import { meowMsgError, meowNoticeError } from "@/utils/message";
import router from "@/routers/index";
import { useUserStore } from "@/stores/index";
import { getToken } from "@/utils/storage";
import { LOGIN_URL } from "@/config";
// 返回值类型
export interface Result<T = any> {
  code: number;
  msg: string;
  data: T;
}
// 只有请求封装用的MM，方便简写
class MM {
  private instance: AxiosInstance;
  // 初始化
  constructor(config: AxiosRequestConfig) {
    // 实例化axios
    this.instance = axios.create(config);
    // 配置拦截器
    this.interceptors();
  }

  // 拦截器
  private interceptors() {
    // 请求发送之前的拦截器：携带token
    // @ts-ignore
    this.instance.interceptors.request.use(
      (config) => this.handleRequest(config),
      (error) => this.handleRequestError(error)
    );

    this.instance.interceptors.response.use(
      (response) => this.handleResponse(response),
      (error) => this.handleResponseError(error)
    );
  }
  private async handleRequest(config: InternalAxiosRequestConfig) {
    console.log("发送请求", config);
    // 获取token
    const token = getToken();
    // 如果实现挤下线功能，需要用户绑定一个uuid，uuid发生变化，后端将数据进行处理[直接使用Sa-Token框架也阔以]
    if (token) {
      config.headers!["Authorization"] = "Bearer " + token;
    }
    return config;
  }
  private async handleRequestError(error: any) {
    error.data = {};
    error.data.msg = "服务器异常，请联系管理员🌻";
    return error;
  }
  private async handleResponse(response: AxiosResponse) {
    console.log("获得数据", response);
    const status = response.data.status || response.data.code; // 后端返回数据状态
    if (status == 200) {
      // 服务器连接状态，非后端返回的status 或者 code
      // 这里的后端可能是code OR status 和 msg OR message需要看后端传递的是什么？
      // console.log("200状态", status);
      return response.data;
    } else if (status == 401) {
      const userStore = useUserStore();
      try {
        await userStore.postToken();
        return await this.instance(response.config);
      } catch (error) {
        userStore.setToken(null); // 清空token必须使用这个，不能使用session清空，因为登录的时候js会获取一遍token还会存在。
        meowMsgError("登录身份过期，请重新登录🌻");
        router.replace(LOGIN_URL);
        return Promise.reject(response.data);
      }
    } else {
      // console.log("后端返回数据：", res.data.msg)
      meowNoticeError(
        response.data.message + "🌻" || "服务器偷偷跑到火星去玩了🌻"
      );
      return Promise.reject(
        response.data.message + "🌻" || "服务器偷偷跑到火星去玩了🌻"
      ); // 可以将异常信息延续到页面中处理，使用try{}catch(error){};
    }
  }
  private async handleResponseError(error: any) {
    // 处理网络错误，不是服务器响应的数据
    console.log("获取错误", error);

    let data: { code: number; message: string } = {
      code: 400,
      message: errorCode[error.status] || "连接到服务器失败🌻",
    };
    if (error && error.response) {
      if (error.status == "401") {
        const userStore = useUserStore();
        await userStore.postToken();
        return await this.instance(error.response.config);
      } else if (error.status == "403") {
        const userStore = useUserStore();
        userStore.setToken(null); // 清空token必须使用这个，不能使用session清空，因为登录的时候js会获取一遍token还会存在。
      } else if (error.status == "405") {
        data.code = 405;
        data.message = "连接到服务器失败🌻";
      } else {
        data.code = error.response.data.code;
        data.message = error.response.data.message;
        meowNoticeError(error.response.data.message);
      }
    } else {
      meowMsgError(data.message);
    }
    // meowMsgError(error.response.data.message || error.data.msg);
    return Promise.reject(data); // 将错误返回给 try{} catch(){} 中进行捕获，就算不进行捕获，上方 res.data.status != 200 也会抛出提示。
  }
  // Get请求
  async get<T = Result>(url: string, params?: object): Promise<T> {
    return await this.instance.get(url, { params });
  }
  // Post请求
  async post<T = Result>(url: string, data?: object): Promise<T> {
    return await this.instance.post(url, data);
  }
  // Put请求
  async put<T = Result>(url: string, data?: object): Promise<T> {
    return await this.instance.put(url, data);
  }
  // Delete请求 /yu/role/1
  async delete<T = Result>(url: string): Promise<T> {
    return await this.instance.delete(url);
  }
  // 图片上传
  async upload<T = Result>(url: string, formData?: object): Promise<T> {
    return await this.instance.post(url, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
  }
  // 导出Excel
  async exportExcel<T = Result>(url: string, params?: object): Promise<T> {
    return await axios.get(import.meta.env.VITE_SERVER + url, {
      params,
      headers: {
        Accept: "application/vnd.ms-excel",
        Authorization: "Bearer " + getToken(),
      },
      responseType: "blob",
    });
  }
  // 下载
  async download<T = Result>(url: string, data?: object): Promise<T> {
    return await axios.post(import.meta.env.VITE_SERVER + url, data, {
      headers: {
        Authorization: "Bearer " + getToken(),
      },
      responseType: "blob",
    });
  }
}
const errorCode: Record<number, string> = {
  100: "100 Continue", //继续
  101: "101 Switching Protocols", //分组交换协议
  200: "200 OK", //OK
  201: "201 Created", //被创建
  202: "202 Accepted", //被采纳
  203: "203 Non - Authoritative Information", //非授权信息
  204: "204 No Content", //无内容
  205: "205 Reset Content", //重置内容
  206: "206 Partial Content", //部分内容
  300: "300 Multiple Choices", //多选项
  301: "301 Moved Permanently", //永久地传送
  302: "302 Found", //找到
  303: "303 See Other", //参见其他
  304: "304 Not Modified", //未改动
  305: "305 Use Proxy", //使用代理
  307: "307 Temporary Redirect", //暂时重定向
  400: "400 Bad Request", //错误请求
  401: "401 Unauthorized", //未授权
  402: "402 Payment Required", //要求付费
  403: "403 Forbidden", //禁止
  404: "404 Not Found", //未找到
  405: "405 Method Not Allowed", //不允许的方法
  406: "406 Not Acceptable", //不被采纳
  407: "407 Proxy Authentication Required", //要求代理授权
  408: "408 Request Time - out", //请求超时
  409: "409 Conflict", //冲突
  410: "410 Gone", //过期的
  411: "411 Length Required", //要求的长度
  412: "412 Precondition Failed", //前提不成立
  413: "413 Request Entity Too Large", //请求实例太大
  414: "414 Request - URI Too Large", //请求URI太大
  415: "415 Unsupported Media Type", //不支持的媒体类型
  416: "416 Requested range not satisfiable", //无法满足的请求范围
  417: "417 Expectation Failed", //失败的预期
  500: "500 Internal Server Error", //内部服务器错误
  501: "501 Not Implemented", //未被使用
  502: "502 Bad Gateway", //网关错误
  503: "503 Service Unavailable", //不可用的服务
  504: "504 Gateway Time - out", //网关超时
  505: "505", //HTTP版本未被支持
};
export default MM; // 实例化axios
