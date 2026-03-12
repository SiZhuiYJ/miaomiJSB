/**
 * @file 打卡模块 HTTP 配置
 * @description 配置打卡模块专用的 HTTP 实例，包含基础路径、并发限制、重试机制等
 */

import createHttp from '@/libs/http';
import { API_BASE_URL } from '@/config';

/**
 * 打卡模块专用的 HTTP 请求实例
 */
export const http = createHttp(API_BASE_URL, {
    concurrencyLimit: 10,           // 最大并发请求数
    retryCount: 2,                  // 请求失败重试次数
    retryDelay: 1000,               // 重试延迟时间 (ms)
    preventDuplicateInterval: 300,  // 防抖间隔时间 (ms)，防止重复提交
});

export default http;
