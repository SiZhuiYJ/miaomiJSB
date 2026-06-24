<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';

interface BingImage {
    startdate: string;
    fullstartdate: string;
    enddate: string;
    url: string;
    urlbase: string;
    copyright: string;
    copyrightlink: string;
    title?: string;
    wp: boolean;
    hsh: string;
}

interface BingImageArchiveResponse {
    images: BingImage[];
}

interface BingImageItem extends BingImage {
    imageUrl: string;
    searchUrl: string;
    displayDate: string;
}

const BING_HOST = 'https://cn.bing.com';
const BING_PROXY_PREFIX = '/bing';
const BING_API_ENDPOINT = `${BING_PROXY_PREFIX}/HPImageArchive.aspx`;

const imageList = ref<BingImageItem[]>([]);
const activeIndex = ref(0);
const historyCount = ref(8);
const offset = ref(0);
const isLoading = ref(false);
const errorMessage = ref('');
let abortController: AbortController | null = null;

const activeImage = computed(() => imageList.value[activeIndex.value]);

const formatDate = (dateText: string) => {
    if (!/^\d{8}$/.test(dateText)) return dateText;

    const year = dateText.slice(0, 4);
    const month = dateText.slice(4, 6);
    const day = dateText.slice(6, 8);

    return `${year}-${month}-${day}`;
};

const toAbsoluteBingUrl = (url: string) => {
    if (/^https?:\/\//i.test(url)) return url;

    return `${BING_HOST}${url}`;
};

const toProxiedBingUrl = (url: string) => {
    const absoluteUrl = toAbsoluteBingUrl(url);
    const parsedUrl = new URL(absoluteUrl);

    return `${BING_PROXY_PREFIX}${parsedUrl.pathname}${parsedUrl.search}`;
};

const normalizeImage = (image: BingImage): BingImageItem => ({
    ...image,
    imageUrl: toProxiedBingUrl(image.url),
    searchUrl: toAbsoluteBingUrl(image.copyrightlink),
    displayDate: formatDate(image.startdate),
});

const buildApiUrl = () => {
    const safeOffset = Math.min(Math.max(Number(offset.value) || 0, 0), 16);
    const safeCount = Math.min(Math.max(Number(historyCount.value) || 1, 1), 8);
    const params = new URLSearchParams({
        format: 'js',
        idx: String(safeOffset),
        n: String(safeCount),
        mkt: 'zh-CN',
    });

    return `${BING_API_ENDPOINT}?${params.toString()}`;
};

const requestBingImages = async (signal: AbortSignal) => {
    const response = await fetch(buildApiUrl(), { signal });

    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }

    const data = await response.json() as BingImageArchiveResponse;

    if (!Array.isArray(data.images) || data.images.length === 0) {
        throw new Error('Bing did not return image data.');
    }

    return data.images.map(normalizeImage);
};

const fetchImages = async () => {
    abortController?.abort();
    const currentController = new AbortController();
    abortController = currentController;
    isLoading.value = true;
    errorMessage.value = '';

    try {
        imageList.value = await requestBingImages(currentController.signal);
        activeIndex.value = 0;
    } catch (error) {
        if (currentController.signal.aborted) return;

        errorMessage.value = error instanceof Error ? error.message : '加载 Bing 图片失败';
        imageList.value = [];
    } finally {
        if (abortController === currentController) {
            abortController = null;
        }

        if (!currentController.signal.aborted) {
            isLoading.value = false;
        }
    }
};

const selectImage = (index: number) => {
    activeIndex.value = index;
};

onMounted(fetchImages);

onUnmounted(() => {
    abortController?.abort();
});
</script>

<template>
    <main class="bing-page">
        <section class="hero" :class="{ 'is-loading': isLoading }">
            <img v-if="activeImage" class="hero-image" :src="activeImage.imageUrl" :alt="activeImage.copyright" />
            <div v-else class="hero-placeholder">
                <span>{{ isLoading ? '正在获取 Bing 图片...' : '暂无图片' }}</span>
            </div>

            <div class="hero-panel">
                <p class="eyebrow">Bing Daily Image</p>
                <h1>{{ activeImage?.title || '获取 Bing 每日图片' }}</h1>
                <p class="copyright">{{ activeImage?.copyright || '使用 HPImageArchive 接口读取每日图片信息。' }}</p>

                <div class="meta" v-if="activeImage">
                    <span>{{ activeImage.displayDate }}</span>
                    <span>{{ activeImage.wp ? '可用作壁纸' : '图片仅供预览' }}</span>
                </div>

                <div class="controls" aria-label="Bing image controls">
                    <label>
                        数量
                        <select v-model.number="historyCount" :disabled="isLoading">
                            <option v-for="count in 8" :key="count" :value="count">{{ count }}</option>
                        </select>
                    </label>

                    <label>
                        idx
                        <input v-model.number="offset" :disabled="isLoading" type="number" min="0" max="16" />
                    </label>

                    <button type="button" :disabled="isLoading" @click="fetchImages">
                        {{ isLoading ? '获取中...' : '获取图片' }}
                    </button>
                </div>

                <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

                <div class="actions" v-if="activeImage">
                    <a :href="activeImage.imageUrl" target="_blank" rel="noreferrer">打开原图</a>
                    <a :href="activeImage.searchUrl" target="_blank" rel="noreferrer">查看说明</a>
                </div>
            </div>
        </section>

        <section class="gallery" aria-label="Bing image gallery">
            <button v-for="(image, index) in imageList" :key="image.hsh" class="thumb"
                :class="{ 'is-active': index === activeIndex }" type="button" @click="selectImage(index)">
                <img :src="image.imageUrl" :alt="image.copyright" />
                <span>{{ image.displayDate }}</span>
            </button>
        </section>
    </main>
</template>

<style scoped lang="scss">
.bing-page {
    min-height: 100vh;
    width: 100%;
    background: #f5f7f6;
    color: #151816;
}

.hero {
    position: relative;
    min-height: 100vh;
    display: flex;
    align-items: flex-end;
    overflow: hidden;
    padding-bottom: 156px;
    isolation: isolate;
}

.hero::after {
    content: '';
    position: absolute;
    inset: 0;
    z-index: -1;
    background: linear-gradient(180deg, rgba(0, 0, 0, 0.08), rgba(0, 0, 0, 0.72));
}

.hero-image,
.hero-placeholder {
    position: absolute;
    inset: 0;
    z-index: -2;
    width: 100%;
    height: 100%;
}

.hero-image {
    object-fit: cover;
    transform: scale(1.02);
    transition: opacity 0.25s ease;
}

.is-loading .hero-image {
    opacity: 0.72;
}

.hero-placeholder {
    display: grid;
    place-items: center;
    background: linear-gradient(135deg, #e8ece8, #cdd8d2);
    color: #415047;
    font-size: 18px;
}

.hero-panel {
    width: min(720px, calc(100% - 32px));
    margin: 0 0 clamp(18px, 4vw, 42px) clamp(16px, 7vw, 80px);
    padding: clamp(18px, 3vw, 28px);
    border: 1px solid rgba(255, 255, 255, 0.22);
    border-radius: 8px;
    background: rgba(10, 14, 12, 0.56);
    color: #fff;
    backdrop-filter: blur(16px);
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.28);
}

.eyebrow {
    margin-bottom: 8px;
    color: #7ee0ae;
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

h1 {
    margin: 0;
    font-size: clamp(30px, 5vw, 56px);
    line-height: 1.08;
}

.copyright {
    margin-top: 14px;
    color: rgba(255, 255, 255, 0.86);
    font-size: 15px;
    line-height: 1.7;
}

.meta {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 16px;

    span {
        border: 1px solid rgba(255, 255, 255, 0.22);
        border-radius: 999px;
        padding: 6px 10px;
        background: rgba(255, 255, 255, 0.1);
        font-size: 13px;
    }
}

.controls {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-end;
    gap: 12px;
    margin-top: 22px;
}

label {
    display: grid;
    gap: 6px;
    color: rgba(255, 255, 255, 0.72);
    font-size: 12px;
}

select,
input,
button {
    height: 40px;
    border: 0;
    border-radius: 6px;
}

select,
input {
    min-width: 84px;
    padding: 0 12px;
    color: #121512;
    background: #fff;
}

button {
    padding: 0 18px;
    color: #08110c;
    background: #7ee0ae;
    font-weight: 700;
    cursor: pointer;

    &:disabled {
        cursor: not-allowed;
        opacity: 0.62;
    }
}

.error {
    margin-top: 14px;
    color: #ffd6d6;
    font-size: 14px;
}

.actions {
    display: flex;
    flex-wrap: wrap;
    gap: 14px;
    margin-top: 18px;

    a {
        color: #b9ffd8;
        font-size: 14px;
        text-decoration: none;

        &:hover {
            text-decoration: underline;
        }
    }
}

.gallery {
    position: fixed;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 5;
    display: flex;
    gap: 12px;
    overflow-x: auto;
    overflow-y: hidden;
    padding: 16px 18px 18px;
    background: linear-gradient(180deg, rgba(8, 11, 9, 0), rgba(8, 11, 9, 0.88) 28%, rgba(8, 11, 9, 0.96));
    scroll-padding: 18px;
    scroll-snap-type: x mandatory;
    -webkit-overflow-scrolling: touch;

    &::-webkit-scrollbar {
        height: 10px;
    }

    &::-webkit-scrollbar-track {
        background: rgba(255, 255, 255, 0.16);
    }

    &::-webkit-scrollbar-thumb {
        border: 2px solid rgba(8, 11, 9, 0.96);
        border-radius: 999px;
        background: rgba(126, 224, 174, 0.9);
    }
}

.thumb {
    position: relative;
    flex: 0 0 clamp(160px, 19vw, 260px);
    height: clamp(88px, 10vw, 132px);
    overflow: hidden;
    border: 2px solid transparent;
    border-radius: 8px;
    padding: 0;
    background: #dfe5e1;
    cursor: pointer;
    scroll-snap-align: start;
    box-shadow: 0 12px 28px rgba(0, 0, 0, 0.28);

    &.is-active {
        border-color: #7ee0ae;
        box-shadow: 0 0 0 3px rgba(126, 224, 174, 0.26), 0 16px 34px rgba(0, 0, 0, 0.34);
    }

    img {
        display: block;
        width: 100%;
        height: 100%;
        object-fit: cover;
        transition: transform 0.2s ease;
    }

    span {
        position: absolute;
        left: 10px;
        bottom: 10px;
        border-radius: 999px;
        padding: 5px 9px;
        color: #fff;
        background: rgba(0, 0, 0, 0.54);
        font-size: 12px;
    }

    &:hover img {
        transform: scale(1.04);
    }
}

@media (max-width: 640px) {
    .hero {
        min-height: 100vh;
        padding-bottom: 132px;
    }

    .hero-panel {
        width: calc(100% - 24px);
        margin: 0 auto 16px;
    }

    .controls {
        align-items: stretch;

        label,
        button {
            flex: 1 1 100%;
        }

        select,
        input {
            width: 100%;
        }
    }

    .gallery {
        gap: 10px;
        padding: 12px 12px 14px;
        scroll-padding: 12px;
    }

    .thumb {
        flex-basis: 156px;
        height: 88px;
    }
}
</style>
