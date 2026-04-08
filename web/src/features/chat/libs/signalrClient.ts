const SIGNALR_SCRIPT_SRC =
  'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js';

type SignalRNamespace = {
  HubConnectionBuilder: new () => {
    withUrl: (
      url: string,
      options?: { accessTokenFactory?: () => string; transport?: number },
    ) => any;
    withAutomaticReconnect: () => any;
    build: () => any;
  };
  HttpTransportType: {
    WebSockets: number;
    LongPolling: number;
  };
  LogLevel: {
    Warning: number;
  };
};

declare global {
  interface Window {
    signalR?: SignalRNamespace;
  }
}

let loadingPromise: Promise<SignalRNamespace> | null = null;

export async function loadSignalR(): Promise<SignalRNamespace> {
  if (window.signalR) return window.signalR;
  if (loadingPromise) return loadingPromise;

  loadingPromise = new Promise<SignalRNamespace>((resolve, reject) => {
    const script = document.createElement('script');
    script.src = SIGNALR_SCRIPT_SRC;
    script.async = true;
    script.onload = () => {
      if (window.signalR) {
        resolve(window.signalR);
      } else {
        reject(new Error('SignalR 脚本已加载但未找到 window.signalR'));
      }
    };
    script.onerror = () => reject(new Error('SignalR 脚本加载失败'));
    document.head.appendChild(script);
  });

  return loadingPromise;
}
