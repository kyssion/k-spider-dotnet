import type { Me } from './types'
import { ApiError } from './errors'

const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS'])
const AUTH_PATHS = ['/api/auth/login', '/api/auth/me']

let csrfToken: string | null = null

interface ApiErrorPayload {
  error: string
  message?: string
}

export function setCsrfToken(token: string | null): void {
  csrfToken = token
}

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
    if (
      retryOnCsrf &&
      response.status === 400 &&
      apiError.code === 'invalid_csrf' &&
      !AUTH_PATHS.includes(path)
    ) {
      await refreshCsrfToken()
      return apiRequest<T>(path, init, false)
    }

    if (response.status === 401 && !path.startsWith('/api/auth/')) {
      redirectToLogin()
    }

    throw apiError
  }

  return payload as T
}

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

function redirectToLogin(): void {
  if (typeof window === 'undefined' || window.location.pathname === '/login') {
    return
  }
  const returnUrl = `${window.location.pathname}${window.location.search}`
  window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`)
}

function toApiError(status: number, payload: unknown): ApiError {
  if (isApiErrorPayload(payload)) {
    return new ApiError(status, payload.error, payload.message)
  }
  return new ApiError(status, `http_${status}`)
}

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

function isApiErrorPayload(value: unknown): value is ApiErrorPayload {
  return (
    typeof value === 'object' &&
    value !== null &&
    'error' in value &&
    typeof (value as { error?: unknown }).error === 'string'
  )
}
