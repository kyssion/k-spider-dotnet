import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createUser, listUsers, patchUser, resetPassword } from '../../api/endpoints/users'
import type { UserCreateInput, UserPatchInput } from '../../api/types'
import { queryKeys } from '../../lib/query-keys'

export function useUsers() {
  return useQuery({
    queryKey: queryKeys.users,
    queryFn: listUsers,
    staleTime: 10_000,
  })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UserCreateInput) => createUser(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.users }),
  })
}

export function usePatchUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { id: number; patch: UserPatchInput }) => patchUser(input.id, input.patch),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.users }),
  })
}

export function useResetPassword() {
  return useMutation({
    mutationFn: (input: { id: number; newPassword: string }) =>
      resetPassword(input.id, input.newPassword),
  })
}
