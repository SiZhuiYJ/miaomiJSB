/**
 * 图片转换工具：将各种图片格式转换为 WebP
 *
 * 特性：
 * - 接受 `File | Blob | string(URL)` 作为输入
 * - 支持设置 `quality`（0-1）和 `maxWidth` / `maxHeight`，保留宽高比
 * - 支持返回 `Blob`、`DataURL` 或 `File`
 *
 * 限制：
 * - 浏览器端实现依赖 canvas，不能保留 GIF 的动画帧（会只取第一帧）
 * - 跨域图片需要服务器设置 `Access-Control-Allow-Origin`，否则可能无法绘制到 canvas
 */
export type ConvertOptions = {
    quality?: number; // 0 - 1
    maxWidth?: number;
    maxHeight?: number;
    output?: 'blob' | 'dataURL' | 'file';
    fileName?: string; // used when output==='file'
};
/**
 * 获取 Blob 对象，如果 input 是 URL，则从 URL 获取 Blob
 * @param input - File | Blob | string (URL)
 * @returns Promise<Blob> - 获取到的 Blob 对象
 */
async function fetchToBlobIfNeeded(input: File | Blob | string): Promise<Blob> {
    if (typeof input === 'string') {
        const res = await fetch(input);
        if (!res.ok) throw new Error(`Failed to fetch image: ${res.status}`);
        return await res.blob();
    }
    return input as Blob;
}

/**
 * 创建 ImageBitmap 或 HTMLImageElement
 * @param blob - Blob 对象
 * @returns Promise<ImageBitmap | HTMLImageElement> - 创建的 ImageBitmap 或 HTMLImageElement
 */
async function createImageBitmapOrElement(blob: Blob): Promise<ImageBitmap | HTMLImageElement> {
    // Prefer createImageBitmap when available (faster, handles orientation better in some browsers)
    if (typeof createImageBitmap === 'function') {
        try {
            return await createImageBitmap(blob);
        } catch (e) {
            // fallback to Image element
        }
    }

    return await new Promise<HTMLImageElement>((resolve, reject) => {
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = () => {
            URL.revokeObjectURL(url);
            resolve(img);
        };
        img.onerror = () => {
            URL.revokeObjectURL(url);
            reject(new Error('Failed to load image element'));
        };
        img.src = url;
    });
}

/**
 * 计算调整后的宽高，保持宽高比
 * @param srcWidth - 原始宽度
 * @param srcHeight - 原始高度
 * @param maxWidth - 最大宽度
 * @param maxHeight - 最大高度
 * @returns { w: number, h: number } - 调整后的宽高
 */
function calcSize(srcWidth: number, srcHeight: number, maxWidth?: number, maxHeight?: number) {
    let w = srcWidth;
    let h = srcHeight;
    if (maxWidth && w > maxWidth) {
        const ratio = maxWidth / w;
        w = Math.round(w * ratio);
        h = Math.round(h * ratio);
    }
    if (maxHeight && h > maxHeight) {
        const ratio = maxHeight / h;
        w = Math.round(w * ratio);
        h = Math.round(h * ratio);
    }
    return { w, h };
}

/**
 * 将图片转换为 WebP（返回 Blob）
 * @param input - File | Blob | string (URL)
 * @param options - 转换选项
 * @returns Promise<Blob> - 转换后的 WebP Blob
 */
export async function convertToWebPBlob(input: File | Blob | string, options: ConvertOptions = {}): Promise<Blob> {
    const { quality = 0.8, maxWidth, maxHeight } = options;
    const blob = await fetchToBlobIfNeeded(input);

    // For GIFs: canvas will draw only the first frame (animated GIFs will be flattened)
    const imageOrBitmap = await createImageBitmapOrElement(blob);

    let srcWidth: number, srcHeight: number;
    if ('width' in imageOrBitmap && 'height' in imageOrBitmap) {
        srcWidth = (imageOrBitmap as any).width;
        srcHeight = (imageOrBitmap as any).height;
    } else {
        // Fallback: use blob URL to measure
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.crossOrigin = 'anonymous';
        await new Promise<void>((resolve, reject) => {
            img.onload = () => resolve();
            img.onerror = () => reject(new Error('Failed to measure image'));
            img.src = url;
        });
        srcWidth = img.width;
        srcHeight = img.height;
        URL.revokeObjectURL(url);
    }

    const { w, h } = calcSize(srcWidth, srcHeight, maxWidth, maxHeight);

    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('Canvas 2D context not available');

    // draw using ImageBitmap or HTMLImageElement
    if ((imageOrBitmap as ImageBitmap).close) {
        // ImageBitmap
        ctx.drawImage(imageOrBitmap as ImageBitmap, 0, 0, w, h);
        try {
            // close imageBitmap to free memory
            (imageOrBitmap as ImageBitmap).close();
        } catch (e) { }
    } else {
        ctx.drawImage(imageOrBitmap as HTMLImageElement, 0, 0, w, h);
    }

    return await new Promise<Blob>((resolve, reject) => {
        // toBlob supports 'image/webp' in modern browsers
        canvas.toBlob(
            (b) => {
                if (!b) reject(new Error('Conversion to WebP failed'));
                else resolve(b);
            },
            'image/webp',
            Math.max(0, Math.min(1, quality))
        );
    });
}

/**
 * 主函数：根据 options.output 返回不同类型
 * @param input - File | Blob | string (URL)
 * @param options - 转换选项，包含 output 类型和 fileName（当 output==='file' 时使用）
 * @returns Promise<Blob | string | File> - 根据 output 返回 Blob、DataURL 或 File
 */
export async function convertToWebP(input: File | Blob | string, options: ConvertOptions = {}): Promise<Blob | string | File> {
    const { output = 'blob', fileName = 'image.webp' } = options;
    const blob = await convertToWebPBlob(input, options);
    if (output === 'blob') return blob;
    if (output === 'dataURL') {
        return await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result as string);
            reader.onerror = () => reject(new Error('Failed to read blob as DataURL'));
            reader.readAsDataURL(blob);
        });
    }
    // file
    return new File([blob], fileName, { type: 'image/webp' });
}

/**
 * Usage example:
 *
 * const fileInput = document.querySelector('input[type=file]');
 * fileInput.onchange = async () => {
 *   const file = fileInput.files[0];
 *   const webpBlob = await convertToWebP(file, { quality: 0.9, maxWidth: 1200 });
 *   // upload webpBlob or create URL: URL.createObjectURL(webpBlob)
 * }
 */

export default convertToWebP;
