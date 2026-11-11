import type { Me } from './types'
import { ApiError } from './errors'

/** 不需要 CSRF 的方法；写方法才注入 token。 */
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS'])

/** 认证入口本身不参与 401 自动跳转，由登录页/路由守卫处理。 */
const AUTH_PATHS = ['/api/auth/login', '/api/auth/me']

/**
 * CSRF token 只保存在模块内存。
 *
 * 不写 localStorage：避免 XSS 直接读取；刷新页面后由 /api/auth/me 重新注入。
 */
let csrfToken: string | null = null

/**
 * 登录成功或 /auth/me 返回后设置 token；登出时清空。
 * @param token 新 token；null 表示清空。
 */
export function setCsrfToken(token: string | null): void {
  csrfToken = token
}

/**
 * 拼接查询字符串。
 *
 * undefined / null / 空串会被跳过，避免把无效筛选发给后端。
 * @param params 键值筛选集合。
 * @returns 以 ? 开头的查询串；没有有效参数时返回空串。
 */
export function buildQuery(
  params: Record<string, string | number | boolean | null | undefined>,
): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') {
      continue
    }
    search.set(key, String(value))
  }
  const query = search.toString()
  return query ? `?${query}` : ''
}

/**
 * 唯一 HTTP 出口。
 *
 * 统一处理同源 Cookie、JSON、CSRF 注入、错误归一化与 401。
 * invalid_csrf 只重试一次：后端在写库前拒绝，重试不会重复业务写入。
 * @param path 以 /api 开头的同源路径。
 * @param init fetch 选项；body 为 JSON 字符串时自动补 Content-Type。
 * @param retryOnCsrf 是否允许在 invalid_csrf 时刷新 token 后重试一次。
 * @returns 解析后的响应体；204 返回 undefined。
 */
export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  retryOnCsrf = true,
): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase()
  const headers = new Headers(init.headers)

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (csrfToken && !SAFE_METHODS.has(method) && !AUTH_PATHS.includes(path)) {
    headers.set('X-CSRF-TOKEN', csrfToken)
  }

  const response = await fetch(path, {
    ...init,
    headers,
    credentials: 'include',
  })

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const payload = parseJson(text)

  if (!response.ok) {
    const apiError = toApiError(response.status, payload)

    // token 过期时刷新一次；登录与 /me 自身不进入重试，避免递归。
    if (
      retryOnCsrf &&
      response.status === 400 &&
      apiError.code === 'invalid_csrf' &&
      !AUTH_PATHS.includes(path)
    ) {
      await refreshCsrfToken()
      return apiRequest<T>(path, init, false)
    }

    // 业务请求 401 说明会话失效；认证入口的 401 交给页面自己处理。
    if (response.status === 401 && !path.startsWith('/api/auth/')) {
      redirectToLogin()
    }

    throw apiError
  }

  return payload as T
}

/** 用 /auth/me 重新获取 token；失败说明会话也不可用，直接走登录兜底。 */
async function refreshCsrfToken(): Promise<void> {
  const response = await fetch('/api/auth/me', {
    credentials: 'include',
    headers: { Accept: 'application/json' },
  })
  if (!response.ok) {
    redirectToLogin()
    throw new ApiError(response.status, 'unauthorized')
  }
  const me = (await response.json()) as Me
  setCsrfToken(me.csrfToken)
}

/**
 * 会话失效兜底。
 *
 * 使用硬跳转而不是只改内存状态，确保刷新后旧页面不会继续发请求；
 * returnUrl 让登录后能回到原位置。
 */
function redirectToLogin(): void {
  if (typeof window === 'undefined' || window.location.pathname === '/login') {
    return
  }
  const returnUrl = `${window.location.pathname}${window.location.search}`
  window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`)
}

/**
 * 把后端错误体转换为 ApiError；非 JSON 错误体保留 HTTP 状态码。
 * @param status HTTP 状态码。
 * @param payload 已解析的响应体，可能是 null 或任意 JSON。
 * @returns 统一的前端错误对象。
 */
function toApiError(status: number, payload: unknown): ApiError {
  if (isApiErrorPayload(payload)) {
    return new ApiError(status, payload.error, payload.message)
  }
  return new ApiError(status, `http_${status}`)
}

/**
 * 容错解析 JSON；代理或网关可能返回 HTML 错误页。
 * @param text 原始响应文本。
 * @returns 解析后的 JSON；空文本或解析失败返回 null。
 */
function parseJson(text: string): unknown {
  if (!text) {
    return null
  }
  try {
    return JSON.parse(text) as unknown
  } catch {
    return null
  }
}

/**
 * 判断响应体是否包含后端错误码。
 * @param value 任意响应体。
 * @returns 具备 string error 字段时返回 true。
 */
function isApiErrorPayload(value: unknown): value is { error: string; message?: string } {
  return (
    typeof value === 'object' &&
    value !== null &&
    'error' in value &&
    typeof (value as { error?: unknown }).error === 'string'
  )
}
