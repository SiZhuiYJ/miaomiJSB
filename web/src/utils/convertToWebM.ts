/**
 * 视频转换工具：将各种视频格式转换为 WebM
 *
 * 特性：
 * - 使用 FFmpeg.wasm 最新版本 (@ffmpeg/ffmpeg ^0.12.x)
 * - 支持多线程处理（使用 @ffmpeg/core-mt）
 * - 实时进度反馈
 * - 异步非阻塞处理
 * - 高效的内存管理
 * - 支持批量转换
 *
 * 技术栈：
 * - @ffmpeg/ffmpeg: 核心 FFmpeg WASM 库
 * - @ffmpeg/util: 实用工具（fetchFile 等）
 * - SharedArrayBuffer: 多线程支持
 */

import { FFmpeg } from '@ffmpeg/ffmpeg';
import { fetchFile } from '@ffmpeg/util';

export type VideoConvertOptions = {
    /** 输出质量 (0-1)，默认 0.8 */
    quality?: number;
    /** 最大宽度，保持宽高比缩放 */
    maxWidth?: number;
    /** 最大高度，保持宽高比缩放 */
    maxHeight?: number;
    /** 视频编码器：'libvpx-vp9' (推荐) 或 'libvpx' */
    videoCodec?: 'libvpx-vp9' | 'libvpx';
    /** 音频编码器：'libopus' (推荐) 或 'libvorbis' */
    audioCodec?: 'libopus' | 'libvorbis';
    /** 帧率，默认保持原样 */
    fps?: number;
    /** 比特率 (例如: '1M', '500k') */
    bitrate?: string;
    /** 是否移除音频 */
    removeAudio?: boolean;
    /** 输出文件名 */
    fileName?: string;
    /** 进度回调函数 (0-100) */
    onProgress?: (progress: number) => void;
    /** 日志回调函数 */
    onLog?: (message: string) => void;
};

export type ConvertResult = {
    /** 转换后的 Blob */
    blob: Blob;
    /** 文件大小（字节） */
    size: number;
    /** 文件名 */
    fileName: string;
    /** MIME 类型 */
    mimeType: string;
    /** Object URL（用于预览） */
    url: string;
};

// FFmpeg 实例缓存，避免重复加载
let ffmpegInstance: FFmpeg | null = null;
let isLoading = false;

/**
 * 创建或获取 FFmpeg 实例（单例模式）
 * 优先使用多线程版本以获得更高性能
 */
async function getFFmpegInstance(): Promise<FFmpeg> {
    if (ffmpegInstance) {
        return ffmpegInstance;
    }

    if (isLoading) {
        // 等待其他加载完成
        return new Promise((resolve, reject) => {
            const checkInterval = setInterval(() => {
                if (!isLoading) {
                    clearInterval(checkInterval);
                    if (ffmpegInstance) {
                        resolve(ffmpegInstance);
                    } else {
                        reject(new Error('FFmpeg loading failed'));
                    }
                }
            }, 100);
        });
    }

    isLoading = true;

    try {
        const ffmpeg = new FFmpeg();

        // 注册日志和进度事件
        ffmpeg.on('log', ({ message }: any) => {
            console.debug('[FFmpeg]', message);
        });

        ffmpegInstance = ffmpeg;
        return ffmpeg;
    } finally {
        isLoading = false;
    }
}

/**
 * 加载 FFmpeg 核心模块
 * 使用本地文件以获得更快的加载速度
 * 注意：开发环境使用单线程版本以避免 COOP/COEP headers 问题
 */
async function loadFFmpegCore(ffmpeg: FFmpeg, onProgress?: (progress: number) => void): Promise<void> {
    if (ffmpeg.loaded) {
        if (onProgress) onProgress(100);
        return;
    }

    // 检测是否支持 SharedArrayBuffer（多线程必需）
    const useMultithread = typeof SharedArrayBuffer !== 'undefined' && crossOriginIsolated;

    // 根据环境选择核心文件
    // 开发环境（HTTP）使用单线程，生产环境（HTTPS）可以使用多线程
    const isDev = import.meta.env.DEV;
    
    let coreURL: string;
    let wasmURL: string | undefined;
    let workerURL: string | undefined;

    if (useMultithread && !isDev) {
        // 生产环境且支持多线程：使用多线程版本
        coreURL = '/ffmpeg/ffmpeg-core.js';
        wasmURL = '/ffmpeg/ffmpeg-core.wasm';
        workerURL = '/ffmpeg/ffmpeg-core.worker.js';
    } else {
        // 开发环境或不支持多线程：使用单线程版本
        // 从 CDN 加载单线程版本（更稳定）
        coreURL = 'https://unpkg.com/@ffmpeg/core@0.12.10/dist/esm/ffmpeg-core.js';
    }

    if (onProgress) onProgress(10);

    try {
        const loadConfig: any = { coreURL };
        if (wasmURL) loadConfig.wasmURL = wasmURL;
        if (workerURL) loadConfig.workerURL = workerURL;

        await ffmpeg.load(loadConfig);
        if (onProgress) onProgress(100);
    } catch (error) {
        console.error('FFmpeg 加载失败:', error);
        throw new Error(`FFmpeg 核心模块加载失败: ${error instanceof Error ? error.message : '未知错误'}`);
    }
}

/**
 * 计算视频缩放参数
 */
function calcScaleParams(
    width: number,
    height: number,
    maxWidth?: number,
    maxHeight?: number
): { scaleFilter?: string; outputWidth?: number; outputHeight?: number } {
    if (!maxWidth && !maxHeight) {
        return {};
    }

    let targetWidth = width;
    let targetHeight = height;

    if (maxWidth && targetWidth > maxWidth) {
        const ratio = maxWidth / targetWidth;
        targetWidth = maxWidth;
        targetHeight = Math.round(height * ratio);
    }

    if (maxHeight && targetHeight > maxHeight) {
        const ratio = maxHeight / targetHeight;
        targetHeight = maxHeight;
        targetWidth = Math.round(width * ratio);
    }

    // 确保宽高为偶数（视频编码要求）
    targetWidth = targetWidth % 2 === 0 ? targetWidth : targetWidth + 1;
    targetHeight = targetHeight % 2 === 0 ? targetHeight : targetHeight + 1;

    return {
        scaleFilter: `scale=${targetWidth}:${targetHeight}`,
        outputWidth: targetWidth,
        outputHeight: targetHeight,
    };
}

/**
 * 根据质量设置比特率
 */
function getBitrateFromQuality(quality: number): string {
    if (quality >= 0.9) return '4M';
    if (quality >= 0.7) return '2M';
    if (quality >= 0.5) return '1M';
    return '500k';
}

/**
 * 从视频中提取元数据（时长、宽高）
 */
async function extractVideoMetadata(file: File): Promise<{ duration: number; width: number; height: number }> {
    return new Promise((resolve, reject) => {
        const video = document.createElement('video');
        video.preload = 'metadata';

        video.onloadedmetadata = () => {
            window.URL.revokeObjectURL(video.src);
            resolve({
                duration: video.duration,
                width: video.videoWidth,
                height: video.videoHeight,
            });
        };

        video.onerror = () => {
            reject(new Error('无法读取视频文件'));
        };

        video.src = URL.createObjectURL(file);
    });
}

/**
 * 构建 FFmpeg 命令参数
 */
function buildFFmpegArgs(
    inputFileName: string,
    outputFileName: string,
    options: VideoConvertOptions,
    metadata: { duration: number; width: number; height: number }
): string[] {
    const args: string[] = ['-i', inputFileName];

    // 视频编码器
    const videoCodec = options.videoCodec || 'libvpx-vp9';
    args.push('-c:v', videoCodec);

    // 音频编码器
    if (!options.removeAudio) {
        const audioCodec = options.audioCodec || 'libopus';
        args.push('-c:a', audioCodec);
    } else {
        args.push('-an');
    }

    // 质量/比特率设置
    const bitrate = options.bitrate || getBitrateFromQuality(options.quality || 0.8);

    if (videoCodec === 'libvpx-vp9') {
        // VP9 使用 crf 和 b:v
        const crf = Math.round(30 - (options.quality || 0.8) * 30); // 0-30，越低质量越好
        args.push('-crf', String(crf), '-b:v', bitrate);
    } else {
        // VP8 使用 qmin/qmax
        const qmin = Math.round(4 - (options.quality || 0.8) * 4);
        const qmax = Math.round(50 - (options.quality || 0.8) * 40);
        args.push('-qmin', String(qmin), '-qmax', String(qmax), '-b:v', bitrate);
    }

    // 缩放滤镜
    const { scaleFilter } = calcScaleParams(metadata.width, metadata.height, options.maxWidth, options.maxHeight);
    if (scaleFilter) {
        args.push('-vf', scaleFilter);
    }

    // 帧率
    if (options.fps) {
        args.push('-r', String(options.fps));
    }

    // CPU 利用率优化（多线程）
    args.push('-cpu-used', '2', '-row-mt', '1');

    // 输出文件
    args.push(outputFileName);

    return args;
}

/**
 * 主函数：将视频转换为 WebM 格式
 *
 * @param file - 输入的视频文件
 * @param options - 转换选项
 * @returns Promise<ConvertResult> - 转换结果
 *
 * @example
 * ```typescript
 * const result = await convertToWebM(videoFile, {
 *   quality: 0.8,
 *   maxWidth: 1920,
 *   onProgress: (p) => console.log(`进度: ${p}%`)
 * });
 * ```
 */
export async function convertToWebM(
    file: File,
    options: VideoConvertOptions = {}
): Promise<ConvertResult> {
    const {
        onProgress,
        onLog,
        fileName = file.name.replace(/\.[^/.]+$/, '') + '.webm',
    } = options;

    try {
        // 1. 获取 FFmpeg 实例并加载核心
        const ffmpeg = await getFFmpegInstance();
        
        if (onProgress) onProgress(2);
        if (onLog) onLog('正在初始化 FFmpeg...');
        
        await loadFFmpegCore(ffmpeg, onProgress ? (p) => {
            // 将加载进度映射到 2-20%
            const mappedProgress = 2 + (p / 100) * 18;
            onProgress(mappedProgress);
        } : undefined);

        // 2. 提取视频元数据
        if (onProgress) onProgress(22);
        if (onLog) onLog('正在读取视频信息...');
        
        const metadata = await extractVideoMetadata(file);

        if (onLog) {
            onLog(`视频信息: ${metadata.width}x${metadata.height}, 时长: ${metadata.duration.toFixed(2)}s`);
        }

        // 3. 写入输入文件到 FFmpeg 虚拟文件系统
        const inputFileName = 'input' + file.name.substring(file.name.lastIndexOf('.'));
        const outputFileName = 'output.webm';

        if (onProgress) onProgress(25);
        if (onLog) onLog('正在加载视频文件到内存...');

        // 使用 fetchFile 高效加载文件
        const fileData = await fetchFile(file);
        
        if (onProgress) onProgress(30);
        await ffmpeg.writeFile(inputFileName, fileData);

        if (onProgress) onProgress(35);
        if (onLog) onLog('开始转换视频...');

        // 4. 构建并执行 FFmpeg 命令
        const args = buildFFmpegArgs(inputFileName, outputFileName, options, metadata);

        if (onLog) onLog(`执行命令: ffmpeg ${args.join(' ')}`);

        // 监听进度 - 使用更激进的进度更新
        let currentProgress = 35;
        const targetProgress = 90;
        const progressInterval = setInterval(() => {
            if (currentProgress < targetProgress) {
                // 渐进式增长，越接近目标越慢
                const increment = Math.max(0.5, (targetProgress - currentProgress) / 20);
                currentProgress += increment;
                if (onProgress) onProgress(Math.min(currentProgress, targetProgress));
            }
        }, 500);

        try {
            await ffmpeg.exec(args);
        } finally {
            clearInterval(progressInterval);
        }

        if (onProgress) onProgress(92);
        if (onLog) onLog('正在读取输出文件...');

        // 5. 读取输出文件
        const outputData = await ffmpeg.readFile(outputFileName);
        
        if (onProgress) onProgress(96);
        const outputBlob = new Blob([outputData as unknown as ArrayBuffer], { type: 'video/webm' });

        // 6. 清理虚拟文件系统
        try {
            await ffmpeg.deleteFile(inputFileName);
            await ffmpeg.deleteFile(outputFileName);
        } catch (e) {
            // 忽略清理错误
        }

        if (onProgress) onProgress(100);
        if (onLog) onLog(`转换完成! 文件大小: ${(outputBlob.size / 1024 / 1024).toFixed(2)}MB`);

        return {
            blob: outputBlob,
            size: outputBlob.size,
            fileName,
            mimeType: 'video/webm',
            url: URL.createObjectURL(outputBlob),
        };
    } catch (error) {
        console.error('视频转换失败:', error);
        throw new Error(`视频转换失败: ${error instanceof Error ? error.message : '未知错误'}`);
    }
}

/**
 * 批量转换视频
 *
 * @param files - 输入的视频文件数组
 * @param options - 转换选项
 * @param onFileProgress - 单个文件进度回调 (index, progress)
 * @returns Promise<ConvertResult[]> - 转换结果数组
 */
export async function convertMultipleToWebM(
    files: File[],
    options: VideoConvertOptions = {},
    onFileProgress?: (index: number, progress: number) => void
): Promise<ConvertResult[]> {
    const results: ConvertResult[] = [];

    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (!file) continue;

        // 创建带有进度回调的选项
        const fileOptions: VideoConvertOptions = {
            ...options,
            onProgress: (progress) => {
                if (onFileProgress) {
                    onFileProgress(i, progress);
                }
            },
        };

        try {
            const result = await convertToWebM(file, fileOptions);
            results.push(result);
        } catch (error) {
            console.error(`转换文件 ${file.name} 失败:`, error);
            // 继续处理其他文件
            results.push({
                blob: new Blob(),
                size: 0,
                fileName: file.name,
                mimeType: 'video/webm',
                url: '',
            });
        }
    }

    return results;
}

/**
 * 释放 FFmpeg 实例，释放内存
 */
export function releaseFFmpeg(): void {
    if (ffmpegInstance) {
        ffmpegInstance = null;
    }
}

export default convertToWebM;
