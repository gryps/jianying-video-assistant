const TOKEN_KEY = "production_workbench_token";

export function storedToken(): string {
  return localStorage.getItem(TOKEN_KEY) ?? "";
}

export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

function apiErrorMessage(payload: unknown, status: number): string {
  const detail = (payload as { detail?: unknown } | null)?.detail;
  if (detail === "Not Found") return "当前功能不可用，请重新启动客户端后再试";
  if (typeof detail === "string") return detail;
  if (Array.isArray(detail)) {
    const messages = detail.map((item) => {
      const error = item as { loc?: unknown[]; msg?: unknown };
      const field = String(error.loc?.at(-1) ?? "");
      const label = { username: "账号", password: "密码", current_password: "当前密码", new_password: "新密码" }[field] ?? field;
      const rawMessage = typeof error.msg === "string" ? error.msg : "输入内容无效";
      const minimum = rawMessage.match(/at least (\d+) characters?/i);
      if (minimum) return `${label}至少需要 ${minimum[1]} 个字符`;
      if (/field required/i.test(rawMessage)) return `请填写${label}`;
      return label ? `${label}：${rawMessage}` : rawMessage;
    });
    if (messages.length) return messages.join("；");
  }
  return `请求失败 (${status})`;
}

export async function api<T>(
  path: string,
  options: RequestInit = {},
  authenticated = true,
): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (authenticated) {
    const token = storedToken();
    if (token) headers.set("Authorization", `Bearer ${token}`);
  }
  const response = await fetch(`/api/v1${path}`, { ...options, headers });
  if (response.status === 401 && authenticated) clearToken();
  if (!response.ok) {
    const payload = await response.json().catch(() => null);
    throw new Error(apiErrorMessage(payload, response.status));
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export async function apiBlob(path: string, options: RequestInit = {}): Promise<Blob> {
  const headers = new Headers(options.headers);
  if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  const token = storedToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const url = `/api/v1${path}`;
  const method = (options.method ?? "GET").toUpperCase();
  if (method !== "GET" || options.body) return fetchBlob(url, options, headers);

  // The fixed WebView2 runtime can abort a large loopback response even after
  // receiving valid 200 headers. File endpoints advertise byte ranges, so read
  // them in small pieces and join the pieces into the browser Blob.
  const chunkSize = 1024 * 1024;
  const parts: Blob[] = [];
  let start = 0;
  let total = Number.POSITIVE_INFINITY;
  let contentType = "application/octet-stream";
  while (start < total) {
    const chunkHeaders = new Headers(headers);
    chunkHeaders.set("Range", `bytes=${start}-${start + chunkSize - 1}`);
    const response = await fetchWithRetry(url, { ...options, headers: chunkHeaders });
    if (response.status === 401) clearToken();
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      throw new Error(apiErrorMessage(payload, response.status));
    }
    if (response.status !== 206) return response.blob();
    const range = response.headers.get("Content-Range")?.match(/^bytes (\d+)-(\d+)\/(\d+)$/i);
    if (!range) throw new Error("本地文件分段信息无效");
    const part = await response.blob();
    if (!part.size) throw new Error("本地文件读取中断");
    parts.push(part);
    contentType = response.headers.get("Content-Type") || contentType;
    start = Number(range[2]) + 1;
    total = Number(range[3]);
  }
  return new Blob(parts, { type: contentType });
}

async function fetchBlob(url: string, options: RequestInit, headers: Headers): Promise<Blob> {
  const response = await fetchWithRetry(url, { ...options, headers });
  if (response.status === 401) clearToken();
  if (!response.ok) {
    const payload = await response.json().catch(() => null);
    throw new Error(apiErrorMessage(payload, response.status));
  }
  return response.blob();
}

async function fetchWithRetry(url: string, options: RequestInit): Promise<Response> {
  try {
    return await fetch(url, options);
  } catch {
    await new Promise(resolve => window.setTimeout(resolve, 200));
    try { return await fetch(url, options); }
    catch { throw new Error("本地服务暂不可用，请重新打开客户端后再试"); }
  }
}
