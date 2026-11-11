import { apiRequest } from '../client'
import type { UserCreateInput, UserPatchInput, UserView } from '../types'

/**
 * 读取账号列表；所有登录用户同权。
 * @returns 账号列表。
 */
export function listUsers(): Promise<UserView[]> {
  return apiRequest<UserView[]>('/api/users')
}

/**
 * 创建账号；用户名/密码规则由 admin 校验。
 * @param input 用户名、初始密码与展示名。
 * @returns 创建成功时无响应体。
 */
export function createUser(input: UserCreateInput): Promise<void> {
  return apiRequest<void>('/api/users', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

/**
 * 修改展示名或启停状态；至少提供一个字段。
 * @param id 账号主键。
 * @param input 展示名或状态修改。
 * @returns 修改成功时无响应体。
 */
export function patchUser(id: number, input: UserPatchInput): Promise<void> {
  return apiRequest<void>(`/api/users/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

/**
 * 重置其他账号密码；自己改密必须走 changePassword。
 * @param id 目标账号主键。
 * @param newPassword 新密码。
 * @returns 重置成功时无响应体。
 */
export function resetPassword(id: number, newPassword: string): Promise<void> {
  return apiRequest<void>(`/api/users/${id}/reset-password`, {
    method: 'POST',
    body: JSON.stringify({ newPassword }),
  })
}
