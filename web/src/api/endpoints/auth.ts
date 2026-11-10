import { apiRequest, setCsrfToken } from '../client'
import type { LoginInput, Me } from '../types'

export async function getMe(): Promise<Me> {
  const me = await apiRequest<Me>('/api/auth/me')
  setCsrfToken(me.csrfToken)
  return me
}

export function login(input: LoginInput): Promise<void> {
  return apiRequest<void>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', { method: 'POST' })
}

export function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return apiRequest<void>('/api/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword }),
  })
}
