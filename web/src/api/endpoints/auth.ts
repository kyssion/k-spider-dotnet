import { apiRequest, setCsrfToken } from '../client'
import type { LoginInput, Me } from '../types'

/**
 * 读取当前登录用户并注入 CSRF token。
 *
 * 这是应用启动/登录后的会话真源；失败 401 由调用方决定跳转。
 * @returns 当前用户与 CSRF token。
 */
export async function getMe(): Promise<Me> {
  const me = await apiRequest<Me>('/api/auth/me')
  setCsrfToken(me.csrfToken)
  return me
}

/**
 * 登录；成功后由调用方失效 me 查询并导航。
 * @param input 用户名、密码与是否免登录。
 * @returns 登录成功时无响应体。
 */
export function login(input: LoginInput): Promise<void> {
  return apiRequest<void>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

/**
 * 清除服务端 Cookie 会话；本地缓存由 auth hook 清理。
 * @returns 登出成功时无响应体。
 */
export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', { method: 'POST' })
}

/**
 * 修改当前账号密码；当前密码错误由后端拒绝。
 * @param currentPassword 当前密码。
 * @param newPassword 新密码；8–128 位。
 * @returns 修改成功时无响应体。
 */
export function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return apiRequest<void>('/api/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword }),
  })
}
