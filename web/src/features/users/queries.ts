import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createUser, listUsers, patchUser, resetPassword } from '../../api/endpoints/users'
import type { UserCreateInput, UserPatchInput } from '../../api/types'
import { queryKeys } from '../../lib/query-keys'

/** 账号列表；10s 内复用。 */
export function useUsers() {
  return useQuery({
    queryKey: queryKeys.users,
    queryFn: listUsers,
    staleTime: 10_000,
  })
}

/** 创建账号；成功后刷新列表。 */
export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UserCreateInput) => createUser(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.users }),
  })
}

/** 修改账号展示名/状态；成功后刷新列表。 */
export function usePatchUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { id: number; patch: UserPatchInput }) => patchUser(input.id, input.patch),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.users }),
  })
}

/** 重置他人密码；不失效列表，密码不在列表中展示。 */
export function useResetPassword() {
  return useMutation({
    mutationFn: (input: { id: number; newPassword: string }) =>
      resetPassword(input.id, input.newPassword),
  })
}
