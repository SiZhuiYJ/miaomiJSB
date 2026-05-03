// utils.ts
import gsap from 'gsap';

/**
 * 构建横向滚动画廊的 GSAP 时间轴
 * @param trackElement - 画廊滚动容器（.gallery-track）
 * @param imgGap - 图片之间的间距（px），默认 40
 * @returns GSAP Timeline
 */
export const buildGalleryTimeline = (trackElement: HTMLElement, imgGap: number = 40): gsap.core.Timeline => {
    const galleryItems = gsap.utils.toArray<HTMLImageElement>(trackElement.querySelectorAll('img'));
    if (!galleryItems.length) return gsap.timeline();

    const timeline = gsap.timeline({ defaults: { ease: 'none' } });

    // 设置 CSS 变量（用于样式中的 gap）
    trackElement.style.setProperty('--img-gap', `${imgGap}px`);

    // 计算移动距离
    const viewportWidth = window.innerWidth + 200;
    const startX = viewportWidth;
    const endX = -trackElement.scrollWidth;
    const moveDistance = startX - endX;

    // 初始状态
    gsap.set(trackElement, { x: startX });
    gsap.set(galleryItems, { scale: 0.5, transformOrigin: 'center center' });

    // 整体横向移动
    timeline.fromTo(trackElement, { x: startX }, { x: endX, duration: 1 }, 0);

    // 为每张图片添加基于滚动进度的缩放动画
    galleryItems.forEach((item) => {
        const itemCenter = item.offsetLeft + item.offsetWidth / 2;
        const centerProgress = gsap.utils.clamp(
            0,
            1,
            (startX + itemCenter - viewportWidth / 2) / moveDistance,
        );
        const scaleDuration = gsap.utils.clamp(
            0.08,
            0.18,
            ((item.offsetWidth + imgGap) / moveDistance) * 1.4,
        );
        const scaleInStart = Math.max(0, centerProgress - scaleDuration);
        const scaleOutEnd = Math.min(1, centerProgress + scaleDuration);

        timeline
            .fromTo(item, { scale: 1.3 }, { scale: 1, duration: centerProgress - scaleInStart }, scaleInStart)
            .to(item, { scale: 0.7, duration: scaleOutEnd - centerProgress }, centerProgress);
    });

    return timeline;
};

/**
 * 构建山脉图片叠化时间轴（示例，根据原有逻辑补充）
 * @param mountainImages - 图片编号数组
 * @returns GSAP Timeline
 */

export function buildCosTimeline(images: number[]) {
    const n = images.length;
    if (n < 3 || n % 2 === 0) {
        console.warn('建议使用奇数个且不少于3张图片，以获得最佳对称效果');
    }

    // 中间索引
    const mid = (n - 1) / 2;

    // 缩放范围（可调整）
    const minFromScale = 0.4;
    const maxFromScale = 1;
    const minToScale = 0.6;
    const maxToScale = 0.9;

    // 水平位置范围（%）
    const leftMin = 8;
    const leftMax = 92;
    const leftStep = (leftMax - leftMin) / (n - 1);

    const tl = gsap.timeline({ defaults: { ease: 'none' } });

    images.forEach((id, i) => {
        const distance = Math.abs(i - mid);

        // 起始缩放：中心最大1，边缘最小0.4（同原效果）
        const fromScale = minFromScale + (maxFromScale - minFromScale) * (mid - distance) / mid;
        // 终点缩放：中心0.9，边缘0.6
        const toScale = minToScale + (maxToScale - minToScale) * (mid - distance) / mid;

        // zIndex：中心最高，边缘最低
        const zIndex = Math.floor((n + 1) / 2) - distance;

        // 目标 left
        const targetLeft = `${leftMin + i * leftStep}%`;

        tl.fromTo(
            `.mountain-img-${id}`,
            {
                scale: fromScale,
                zIndex: zIndex,
                left: '50%',        // 所有图片起始堆叠居中
            },
            {
                scale: toScale,
                left: targetLeft,
            },
            i === 0 ? undefined : '<' // 第一个正常排，后续全部与前一个同时开始
        );
    });

    return tl;
}