import { apiRequest } from '../client'
import type { UserCreateInput, UserPatchInput, UserView } from '../types'

export function listUsers(): Promise<UserView[]> {
  return apiRequest<UserView[]>('/api/users')
}

export function createUser(input: UserCreateInput): Promise<void> {
  return apiRequest<void>('/api/users', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function patchUser(id: number, input: UserPatchInput): Promise<void> {
  return apiRequest<void>(`/api/users/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function resetPassword(id: number, newPassword: string): Promise<void> {
  return apiRequest<void>(`/api/users/${id}/reset-password`, {
    method: 'POST',
    body: JSON.stringify({ newPassword }),
  })
}
