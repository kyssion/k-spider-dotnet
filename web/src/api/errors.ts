/**
 * 前端统一的 API 错误。
 *
 * 只保留稳定错误码与 HTTP 状态，页面按 code/status 决定提示与重试；
 * 不把后端堆栈或内部消息直接透出到 UI。
 */
export class ApiError extends Error {
  readonly status: number
  readonly code: string

  constructor(status: number, code: string, message?: string) {
    super(message ?? code)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

/**
 * 类型守卫：避免页面 catch 后对 unknown 直接取属性。
 * @param error catch 到的未知值。
 * @returns 是 ApiError 时返回 true。
 */
export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}
