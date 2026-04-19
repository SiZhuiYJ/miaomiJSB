export function getVideoDuration(file: File): Promise<number | null> {
  return new Promise((resolve) => {
    const video = document.createElement("video");
    const url = URL.createObjectURL(file);
    let settled = false;

    const cleanup = () => {
      video.onloadedmetadata = null;
      video.onerror = null;
      video.ontimeupdate = null;
      video.removeAttribute("src");
      video.load();
      URL.revokeObjectURL(url);
    };

    const finish = (value: number | null) => {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(value && Number.isFinite(value) && value > 0 ? value : null);
    };

    const resolveFiniteDuration = () => {
      if (Number.isFinite(video.duration) && video.duration > 0) {
        finish(video.duration);
        return;
      }

      // 某些浏览器生成的 WebM 会返回 Infinity，需要通过 seek 到末尾触发时长计算。
      if (video.duration === Number.POSITIVE_INFINITY) {
        const fallbackTimer = window.setTimeout(() => finish(null), 1500);
        video.ontimeupdate = () => {
          window.clearTimeout(fallbackTimer);
          const duration = Number.isFinite(video.duration) ? video.duration : video.currentTime;
          finish(duration);
        };

        try {
          video.currentTime = 1e101;
        } catch {
          window.clearTimeout(fallbackTimer);
          finish(null);
        }
        return;
      }

      finish(null);
    };

    video.onloadedmetadata = resolveFiniteDuration;
    video.onerror = () => finish(null);
    video.preload = "metadata";
    video.src = url;
  });
}
