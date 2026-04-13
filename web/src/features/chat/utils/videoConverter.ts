// utils/videoConverter.ts
import { FFmpeg } from "@ffmpeg/ffmpeg";
import { fetchFile, toBlobURL } from "@ffmpeg/util";

// const ffmpeg = new FFmpeg();
// 定义进度回调类型
export interface ProgressInfo {
  percent: number; // 0-100
  message?: string;
}

export interface ConvertOptions {
  // WebM 编码参数：VP9 质量更高但稍慢，VP8 兼容性好且较快
  codec?: "libvpx-vp9" | "libvpx";
  // 码率控制：建议 CRF (0-63)，值越小质量越好，体积越大[reference:2]
  crf?: number;
  // 编码预设：'veryfast' 速度快体积大，'medium' 平衡，'veryslow' 速度慢体积小
  preset?:
    | "ultrafast"
    | "superfast"
    | "veryfast"
    | "faster"
    | "fast"
    | "medium"
    | "slow"
    | "slower"
    | "veryslow";
  // 音频比特率
  audioBitrate?: string;
  // 视频比特率 (可选，与 CRF 互斥，不建议同时使用)
  videoBitrate?: string;
  // 输出缩放，例如 '1280:-1' 表示宽度 1280 高度自动
  scale?: string;
  // 进度回调
  onProgress?: (progress: ProgressInfo) => void;
}

/**
 * 全视频格式转 WebM 工具函数 (输入 File 输出 File)
 */
export async function convertVideoToWebM(
  inputFile: File,
  options: ConvertOptions = {},
): Promise<File> {
  const {
    codec = "libvpx",
    crf = 30,
    preset = "medium",
    audioBitrate = "128k",
    videoBitrate,
    scale,
    onProgress,
  } = options;

  // 1. 初始化 FFmpeg
  const ffmpeg = new FFmpeg();

  // 2. 注册进度监听[reference:3][reference:4]
  ffmpeg.on("progress", ({ progress }) => {
    const percent = Math.round(progress * 100);
    onProgress?.({ percent, message: `转换进度 ${percent}%` });
  });

  // 3. 日志监听 (方便调试，生产环境可注释)
  ffmpeg.on("log", ({ message }) => {
    console.log("[FFmpeg]", message);
  });

  // 4. 加载 FFmpeg Core (必须指定 worker 与 wasm 路径)
  // const baseURL = "https://unpkg.com/@ffmpeg/core@0.12.6/dist/esm";
  // await ffmpeg.load({
  //   coreURL: await toBlobURL(`${baseURL}/ffmpeg-core.js`, "text/javascript"),
  //   wasmURL: await toBlobURL(`${baseURL}/ffmpeg-core.wasm`, "application/wasm"),
  // });

  // 4. 加载本地 Core 文件（注意路径需与 public 目录对应）
  const baseURL = "/ffmpeg"; // 指向 public/ffmpeg 目录

  await ffmpeg.load({
    coreURL: await toBlobURL(`${baseURL}/ffmpeg-core.js`, "text/javascript"),
    wasmURL: await toBlobURL(`${baseURL}/ffmpeg-core.wasm`, "application/wasm"),
    // 关键！多线程版本必须的 worker 文件
    workerURL: await toBlobURL(
      `${baseURL}/ffmpeg-core.worker.js`,
      "text/javascript",
    ),
  });

  // 5. 将输入文件写入虚拟文件系统
  const inputFileName = `input.${inputFile.name.split(".").pop() || "mp4"}`;
  await ffmpeg.writeFile(inputFileName, await fetchFile(inputFile));

  // 6. 构建 FFmpeg 命令参数
  const outputFileName = "output.webm";
  const args: string[] = [
    "-i",
    inputFileName,
    "-c:v",
    codec, // 视频编码器[reference:5]
    "-c:a",
    "libopus", // 音频编码器 (WebM 推荐 Opus)
    "-b:a",
    audioBitrate, // 音频比特率
    "-preset",
    preset, // 编码预设
  ];

  // 码率控制策略：优先使用 CRF，若指定视频比特率则使用 ABR
  if (videoBitrate) {
    args.push("-b:v", videoBitrate); // 固定码率模式
  } else {
    args.push("-crf", String(crf)); // 恒定质量模式[reference:6]
    args.push("-b:v", "0"); // 完全交由 CRF 控制
  }

  // 分辨率缩放 (可选)
  if (scale) {
    args.push("-vf", `scale=${scale}`);
  }

  // 线程数：0 表示自动根据 CPU 核心数分配[reference:7]
  args.push("-threads", "0");

  args.push(outputFileName);

  // 7. 执行转码
  await ffmpeg.exec(args);

  // 8. 读取输出文件
  const data = (await ffmpeg.readFile(outputFileName)) as Uint8Array;
  const webmBlob = new Blob([data], { type: "video/webm" });

  // 9. 清理与终止
  await ffmpeg.deleteFile(inputFileName);
  await ffmpeg.deleteFile(outputFileName);
  ffmpeg.terminate();

  return new File([webmBlob], inputFile.name.replace(/\.[^.]+$/, ".webm"), {
    type: "video/webm",
    lastModified: Date.now(),
  });
}
