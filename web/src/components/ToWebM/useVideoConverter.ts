// composables/useVideoConverter.ts
import { encode, type EncodeOptions } from 'webcodecs-encoder';

export interface ConversionOptions {
    quality: 'lossless' | 'high' | 'medium' | 'low';
    onProgress?: (percent: number) => void;
}

// WebCodecs 转换方案
export async function convertWithWebCodecs(
    file: File,
    options: ConversionOptions = { quality: 'high' }
): Promise<Blob> {
    const qualityMap: Record<ConversionOptions['quality'], EncodeOptions['video']['quality']> = {
        lossless: 'lossless',
        high: 'high',
        medium: 'medium',
        low: 'low'
    };

    let frameCount = 0;
    let totalFrames = 0;

    // 正确的 API 调用方式：第一个参数是文件，第二个参数是配置
    const encodeOptions: EncodeOptions = {
        outputFormat: 'webm',
        video: {
            codec: 'vp09.00.10.08', // VP9 Profile 0 Level 1.0，更精确的编码器标识
            quality: qualityMap[options.quality],
            // 如果需要指定分辨率，可在此设置 width/height
        },
        audio: {
            codec: 'opus',
            bitrate: 128000,
        },
        onProgress: (info) => {
            if (info.totalFrames) totalFrames = info.totalFrames;
            if (info.encodedFrames !== undefined) {
                frameCount = info.encodedFrames;
                if (totalFrames > 0 && options.onProgress) {
                    options.onProgress(Math.round((frameCount / totalFrames) * 100));
                }
            }
        },
    };

    const result = await encode(file, encodeOptions);
    return result;
}

// ffmpeg.wasm 降级方案（已优化内存管理）
export async function convertWithFFmpeg(
    file: File,
    options: ConversionOptions = { quality: 'high' }
): Promise<Blob> {
    const { FFmpeg } = await import('@ffmpeg/ffmpeg');
    const { fetchFile, toBlobURL } = await import('@ffmpeg/util');

    const ffmpeg = new FFmpeg();

    // 使用 CDN 确保文件正确加载
    const baseURL = '/ffmpeg';
    await ffmpeg.load({
        coreURL: await toBlobURL(`${baseURL}/ffmpeg-core.js`, 'text/javascript'),
        wasmURL: await toBlobURL(`${baseURL}/ffmpeg-core.wasm`, 'application/wasm'),
        workerURL: await toBlobURL(`${baseURL}/ffmpeg-core.worker.js`, 'text/javascript'), // Worker 文件
        // 不指定 workerURL，让 ffmpeg 自动处理，减少出错可能
    });

    ffmpeg.on('progress', ({ progress }) => {
        options.onProgress?.(Math.round(progress * 100));
    });

    ffmpeg.on('log', ({ message }: { message: string }) => console.log('[FFmpeg LOG]', message));

    const crfMap: Record<ConversionOptions['quality'], number> = {
        lossless: 0,
        high: 18,
        medium: 23,
        low: 28
    };
    const crf = crfMap[options.quality];

    try {
        await ffmpeg.writeFile('input.mp4', await fetchFile(file));

        // 添加分辨率限制，防止内存爆炸
        // await ffmpeg.exec([
        //     '-i', 'input.mp4',
        //     '-c:v', 'libvpx-vp9',
        //     '-c:a', 'libopus',
        //     '-crf', String(crf),
        //     '-b:v', '0',
        //     '-deadline', 'realtime',
        //     '-cpu-used', '5',
        //     '-threads', '1',
        //     '-row-mt', '0',
        //     '-vf', 'scale=1280:-2', // 限制宽度不超过1280，高度按比例
        //     'output.webm'
        // ]);
        // await ffmpeg.exec([
        //     "-i", "input.mp4",
        //     "-fflags", "+genpts",
        //     "-f", "webm",
        //     "-preset", "ultrafast",
        //     "-c:v", "libvpx-vp9",// libvpx
        //     "-c:a", "libvorbis",
        //     "-crf", "0",
        //     '-b:v', '0',           // 视频码率
        //     '-b:a', '128k',
        //     "-threads", "1",
        //     "output.webm",
        // ]);

        await ffmpeg.exec([
            "-i", "input.mp4",
            "-fflags", "+genpts",
            "-f", "webm",
            "-preset", "ultrafast",
            "-c:v", "libvpx-vp9",
            "-c:a", "libopus",
            "-crf", "0",
            // '-b:v', '1M',           // 视频码率
            "-threads", "1",
            "output.webm",
        ]);

        const data = await ffmpeg.readFile('output.webm');
        await ffmpeg.deleteFile('input.mp4');
        await ffmpeg.deleteFile('output.webm');

        console.log("dataFile", data)
        const outputBlob = new Blob([data], { type: 'video/webm' });
        console.log("dataBlob", outputBlob)
        return outputBlob
        // 处理返回类型
        // if (data instanceof Uint8Array) {
        //     return new Blob([data], { type: 'video/webm' });
        // } else {
        //     // 兼容其他可能的返回类型
        //     return new Blob([data as BlobPart], { type: 'video/webm' });
        // }
    } catch (error) {
        await ffmpeg.deleteFile('input.mp4').catch(() => { });
        await ffmpeg.deleteFile('output.webm').catch(() => { });
        throw error;
    }
}

// 更准确的 WebCodecs 支持检测
async function checkWebCodecsSupport(): Promise<boolean> {
    if (typeof VideoEncoder === 'undefined' || typeof VideoDecoder === 'undefined') {
        return false;
    }

    // 检测 VP9 编码器是否可用
    try {
        const config = {
            codec: 'vp09.00.10.08',
            width: 640,
            height: 480,
            bitrate: 1_000_000,
        };
        const support = await VideoEncoder.isConfigSupported(config);
        return support.supported === true;
    } catch {
        return false;
    }
}

export async function convertVideo(
    file: File,
    options: ConversionOptions = { quality: 'high' }
): Promise<Blob> {
    const supportsWebCodecs = await checkWebCodecsSupport();
    console.log(`WebCodecs 支持情况: ${supportsWebCodecs}`);

    if (supportsWebCodecs) {
        try {
            console.log('使用 WebCodecs 方案');
            return await convertWithWebCodecs(file, options);
        } catch (error) {
            console.warn('WebCodecs 转换失败，降级到 ffmpeg.wasm:', error);
            return await convertWithFFmpeg(file, options);
        }
    }

    console.log('使用 ffmpeg.wasm 方案');
    return await convertWithFFmpeg(file, options);
}
