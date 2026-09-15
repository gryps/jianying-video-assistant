import type { SourceVideo } from "./types";

type WebViewMessageEvent = { data: unknown };
type DesktopWebView = {
  postMessage: (message: unknown) => void;
  addEventListener: (type: "message", listener: (event: WebViewMessageEvent) => void) => void;
  removeEventListener: (type: "message", listener: (event: WebViewMessageEvent) => void) => void;
};

type DesktopResponse = {
  type?: string;
  requestId?: string;
  cancelled?: boolean;
  sourceDir?: string;
  videos?: SourceVideo[];
  error?: string;
};

function webView(): DesktopWebView | undefined {
  return (window as unknown as { chrome?: { webview?: DesktopWebView } }).chrome?.webview;
}

export function hasDesktopVideoPicker(): boolean {
  return Boolean(webView());
}

export function selectDesktopSourceVideos(): Promise<{ sourceDir: string; videos: SourceVideo[] } | null> {
  const bridge = webView();
  if (!bridge) return Promise.reject(new Error("桌面文件选择器不可用"));
  const activeBridge = bridge;
  const requestId = crypto.randomUUID();
  return new Promise((resolve, reject) => {
    const timeout = window.setTimeout(() => finish(() => reject(new Error("文件选择器响应超时"))), 120_000);
    const onMessage = (event: WebViewMessageEvent) => {
      const message = event.data as DesktopResponse;
      if (message.requestId !== requestId) return;
      if (message.type === "desktop-error") finish(() => reject(new Error(message.error || "文件选择失败")));
      else if (message.type === "source-videos-selected") finish(() => resolve(message.cancelled ? null : { sourceDir: message.sourceDir ?? "", videos: message.videos ?? [] }));
    };
    function finish(action: () => void) {
      window.clearTimeout(timeout);
      activeBridge.removeEventListener("message", onMessage);
      action();
    }
    activeBridge.addEventListener("message", onMessage);
    activeBridge.postMessage({ type: "select-source-videos", requestId });
  });
}

export function openDesktopDirectory(path: string): void {
  webView()?.postMessage({ type: "open-directory", path });
}
