import { reactive } from "vue";
import http from "@/libs/http/file";
import { useAuthStore } from "@/features/auth/stores";

interface QueueItem {
    fileKey: string;
    category: string;
    thumbnailUrl?: string;
    onComplete: (blobUrl: string) => void;
    onThumbnailComplete?: (blobUrl: string) => void;
    onError: (error: Error) => void;
}

interface DownloadTask {
    item: QueueItem;
    priority: "high" | "low";
}

interface BlobUrlRequest {
    onComplete: (blobUrl: string) => void;
    onError: (error: Error) => void;
    isNew?: boolean;
}

const downloadedBlobs = reactive<Record<string, string>>({});
const queue = reactive<DownloadTask[]>([]);
let activeDownloads = 0;

const maxConcurrentDownloads = 2;
const maxRetries = 2;
const retryDelayMs = 800;

async function downloadWithRetry(url: string, retries = maxRetries): Promise<Blob> {
    try {
        return await downloadFile(url);
    } catch (error) {
        if (retries > 0) {
            await new Promise(resolve => setTimeout(resolve, retryDelayMs));
            return downloadWithRetry(url, retries - 1);
        }
        throw error;
    }
}

export function getAuthHeaders(): Record<string, string> {
    const token = useAuthStore().accessToken;
    return token ? { Authorization: `Bearer ${token}` } : {};
}

async function downloadFile(url: string): Promise<Blob> {
    const response = await http.get(url, {
        responseType: "blob",
        headers: getAuthHeaders(),
    });
    if (response.status !== 200) {
        throw new Error(
            `Failed to download file: ${response.status} ${response.statusText}`,
        );
    }
    return response.data as Blob;
}

async function processQueue() {
    if (activeDownloads >= maxConcurrentDownloads || queue.length === 0) {
        return;
    }

    activeDownloads++;

    const taskIndex = queue.findIndex((t) => t.priority === "high");
    const task = taskIndex !== -1 ? queue.splice(taskIndex, 1)[0] : queue.shift();

    if (!task) {
        activeDownloads--;
        return;
    }

    const { item } = task;
    const {
        fileKey,
        category,
        thumbnailUrl,
        onComplete,
        onThumbnailComplete,
        onError,
    } = item;

    const baseUrl = import.meta.env.VITE_API_BASE_URL || "";
    const normalizedBaseUrl = baseUrl.endsWith("/")
        ? baseUrl.slice(0, -1)
        : baseUrl;

    try {
        // --- Thumbnail Handling ---
        if (onThumbnailComplete) {
            let thumbKey: string | undefined;
            let thumbUrl: string | undefined;

            if (category === "video" && thumbnailUrl) {
                thumbKey = thumbnailUrl;
                thumbUrl = `${normalizedBaseUrl}/mm/files/chat/${thumbnailUrl}`;
            }

            if (thumbKey && thumbUrl) {
                if (downloadedBlobs[thumbKey]) {
                    onThumbnailComplete(downloadedBlobs[thumbKey]!);
                } else {
                    const thumbBlob = await downloadWithRetry(thumbUrl);
                    const thumbBlobUrl = URL.createObjectURL(thumbBlob);
                    downloadedBlobs[thumbKey] = thumbBlobUrl;
                    onThumbnailComplete(thumbBlobUrl);
                }
            }
        }

        // --- Main File Handling ---
        if (downloadedBlobs[fileKey]) {
            onComplete(downloadedBlobs[fileKey]);
        } else {
            const fileUrl = `${normalizedBaseUrl}/mm/files/chat/${fileKey}`;
            const blob = await downloadWithRetry(fileUrl);
            const blobUrl = URL.createObjectURL(blob);
            downloadedBlobs[fileKey] = blobUrl;
            onComplete(blobUrl);
        }
    } catch (error) {
        console.error(`Failed to download file ${fileKey}:`, error);
        onError(error as Error);
    } finally {
        activeDownloads--;
        processQueue();
    }
}

function requestDownload(item: QueueItem, isNew: boolean) {
    const { fileKey, thumbnailUrl, category } = item;

    const mainAssetReady = downloadedBlobs[fileKey];
    let thumbAssetReady = true;
    if (category === "video" && thumbnailUrl) {
        thumbAssetReady = !!downloadedBlobs[thumbnailUrl];
    }

    if (mainAssetReady && thumbAssetReady && downloadedBlobs[fileKey]) {
        item.onComplete(downloadedBlobs[fileKey]);
        if (item.onThumbnailComplete) {
            const thumbKey = category === "video" ? thumbnailUrl : undefined;
            if (thumbKey && downloadedBlobs[thumbKey]) {
                item.onThumbnailComplete(downloadedBlobs[thumbKey]);
            }
        }
        return;
    }

    if (queue.some((task) => task.item.fileKey === item.fileKey)) {
        return;
    }

    const priority = isNew ? "high" : "low";
    const task: DownloadTask = { item, priority };

    if (priority === "high") {
        queue.unshift(task);
    } else {
        queue.push(task);
    }

    if (activeDownloads < maxConcurrentDownloads) {
        processQueue();
    }
}

function requestBlobUrl(fileKey: string, options: BlobUrlRequest) {
    requestDownload({
        fileKey,
        category: "asset",
        onComplete: options.onComplete,
        onError: options.onError,
    }, options.isNew ?? false);
}

async function downloadAndSaveFile(fileKey: string, fileName: string) {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || "";
    const normalizedBaseUrl = baseUrl.endsWith("/")
        ? baseUrl.slice(0, -1)
        : baseUrl;
    const url = `${normalizedBaseUrl}/mm/files/chat/${fileKey}`;

    try {
        let blob: Blob;
        if (downloadedBlobs[fileKey]) {
            const blobUrl = downloadedBlobs[fileKey];
            const response = await fetch(blobUrl);
            blob = await response.blob();
        } else {
            ElMessage.info("开始下载...");
            blob = await downloadWithRetry(url);
        }

        const navigatorWithSave = window.navigator as Navigator & {
            msSaveOrOpenBlob?: (blob: Blob, fileName: string) => void;
        };

        if (navigatorWithSave.msSaveOrOpenBlob) {
            navigatorWithSave.msSaveOrOpenBlob(blob, fileName);
        } else {
            const objectUrl = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = objectUrl;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
        }
        ElMessage.success("下载成功");
    } catch (error) {
        console.error("Download error:", error);
        ElMessage.error(`下载失败: ${(error as Error).message}`);
    }
}

export function useFileDownloader() {
    return {
        requestBlobUrl,
        requestDownload,
        downloadAndSaveFile,
    };
}
