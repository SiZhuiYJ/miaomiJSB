// composables/useVideoMetadata.ts
import MediaInfoFactory, { type MediaInfo, type Track } from 'mediainfo.js';

export interface VideoMetadata {
    duration: number;
    width: number;
    height: number;
    frameRate: number;
    fileSize: number;
    codec: string;
    bitRate: number;
    audioCodec: string;
    audioSampleRate: number;
    audioChannels: number;
    pixelFormat: string;
    container: string;
}

// 类型守卫：判断轨道是否为视频轨道
function isVideoTrack(track: Track): track is Track & { '@type': 'Video' } {
    return track['@type'] === 'Video';
}

// 类型守卫：判断轨道是否为音频轨道
function isAudioTrack(track: Track): track is Track & { '@type': 'Audio' } {
    return track['@type'] === 'Audio';
}

// 类型守卫：判断轨道是否为通用轨道
function isGeneralTrack(track: Track): track is Track & { '@type': 'General' } {
    return track['@type'] === 'General';
}

export async function extractMetadataWithMediaInfo(file: File): Promise<VideoMetadata> {
    let mediaInfo: MediaInfo | null = null;
    try {
        mediaInfo = await MediaInfoFactory({
            format: 'object',
            locateFile: () => '/mediainfo/MediaInfoModule.wasm'
        });

        const getSize = () => file.size;
        const readChunk = (size: number, offset: number): Promise<Uint8Array> =>
            new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = (e) => resolve(new Uint8Array(e.target?.result as ArrayBuffer));
                reader.onerror = reject;
                reader.readAsArrayBuffer(file.slice(offset, offset + size));
            });

        const result = await mediaInfo.analyzeData(getSize, readChunk);

        // 安全访问 media 属性
        const tracks = result.media?.track ?? [];

        // 查找各类型轨道
        const videoTrack = tracks.find(isVideoTrack);
        const audioTrack = tracks.find(isAudioTrack);
        const generalTrack = tracks.find(isGeneralTrack);

        // 安全获取属性值，提供默认值
        return {
            duration: parseFloat(generalTrack?.Duration?.toString() ?? generalTrack?.Duration_String4?.toString() ?? '0') || 0,
            width: parseInt(videoTrack?.Width?.toString() ?? '0') || 0,
            height: parseInt(videoTrack?.Height?.toString() ?? '0') || 0,
            frameRate: parseFloat(videoTrack?.FrameRate?.toString() ?? videoTrack?.FrameRate_String?.toString() ?? '0') || 0,
            fileSize: file.size,
            codec: videoTrack?.Format ?? videoTrack?.CodecID ?? videoTrack?.CodecID_String ?? 'Unknown',
            bitRate: parseInt(videoTrack?.BitRate?.toString() ?? videoTrack?.BitRate_String?.toString() ?? '0') || 0,
            audioCodec: audioTrack?.Format ?? audioTrack?.CodecID ?? 'Unknown',
            audioSampleRate: parseInt(audioTrack?.SamplingRate?.toString() ?? audioTrack?.SamplingRate_String?.toString() ?? '0') || 0,
            audioChannels: parseInt(audioTrack?.Channels?.toString() ?? audioTrack?.Channels_String ?? '0') || 0,
            pixelFormat: videoTrack?.PixelAspectRatio?.toString() ?? videoTrack?.PixelAspectRatio_String ?? 'Unknown',
            container: generalTrack?.Format ?? generalTrack?.Format_String ?? 'Unknown',
        };
    } finally {
        // 清理 MediaInfo 实例，避免内存泄漏
        if (mediaInfo?.close) {
            mediaInfo.close();
        }
    }
}

// 原生 video 元素降级方案
export async function extractMetadataBasic(file: File): Promise<Partial<VideoMetadata>> {
    return new Promise((resolve) => {
        const video = document.createElement('video');
        const url = URL.createObjectURL(file);
        video.src = url;

        video.onloadedmetadata = () => {
            resolve({
                duration: video.duration,
                width: video.videoWidth,
                height: video.videoHeight,
                fileSize: file.size,
                frameRate: 0,
                codec: '',
                bitRate: 0,
                audioCodec: '',
                audioSampleRate: 0,
                audioChannels: 0,
                pixelFormat: '',
                container: '',
            });
            URL.revokeObjectURL(url);
        };

        video.onerror = () => {
            URL.revokeObjectURL(url);
            resolve({
                duration: 0,
                width: 0,
                height: 0,
                fileSize: file.size,
            });
        };
    });
}

export async function extractMetadata(file: File): Promise<VideoMetadata> {
    try {
        return await extractMetadataWithMediaInfo(file);
    } catch (error) {
        console.warn('mediainfo.js 解析失败，使用原生API降级:', error);
        const basic = await extractMetadataBasic(file);
        // 确保返回完整的 VideoMetadata 类型
        return {
            duration: basic.duration ?? 0,
            width: basic.width ?? 0,
            height: basic.height ?? 0,
            frameRate: basic.frameRate ?? 0,
            fileSize: basic.fileSize ?? file.size,
            codec: basic.codec ?? 'Unknown',
            bitRate: basic.bitRate ?? 0,
            audioCodec: basic.audioCodec ?? 'Unknown',
            audioSampleRate: basic.audioSampleRate ?? 0,
            audioChannels: basic.audioChannels ?? 0,
            pixelFormat: basic.pixelFormat ?? 'Unknown',
            container: basic.container ?? 'Unknown',
        };
    }
}
