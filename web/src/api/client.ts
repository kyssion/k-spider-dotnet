/** API 客户端 : 同源 /api , 开发期由 vite 代理 */

export type QueryParams = Record<string, string | number | undefined | null>

export async function apiGet<T>(path: string, params?: QueryParams): Promise<T> {
  const qs = params
    ? '?' +
      new URLSearchParams(
        Object.entries(params)
          .filter(([, v]) => v !== undefined && v !== null && v !== '')
          .map(([k, v]) => [k, String(v)]),
      ).toString()
    : ''
  return request<T>(`${path}${qs}`, { method: 'GET' })
}

export async function apiPost<T>(path: string): Promise<T> {
  return request<T>(path, { method: 'POST' })
}

async function request<T>(url: string, init: RequestInit): Promise<T> {
  const res = await fetch(`/api${url}`, init)
  if (!res.ok) {
    const body = await res.text().catch(() => '')
    throw new Error(`${init.method} ${url} → ${res.status} ${body.slice(0, 200)}`)
  }
  return res.json() as Promise<T>
}

/** 带 JSON 请求体的 POST ( 重放的预览 / 提交 ) */
export async function apiPostBody<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`/api${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(`POST ${path} → ${res.status} ${text.slice(0, 200)}`)
  }
  return res.json() as Promise<T>
}
