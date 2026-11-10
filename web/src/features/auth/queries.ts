import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { changePassword, getMe, login, logout } from '../../api/endpoints/auth'
import { setCsrfToken } from '../../api/client'
import type { LoginInput } from '../../api/types'
import { queryKeys } from '../../lib/query-keys'

export function useMe() {
  return useQuery({
    queryKey: queryKeys.auth.me,
    queryFn: getMe,
    retry: false,
    staleTime: 30_000,
  })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: LoginInput) => login(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me })
    },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSettled: () => {
      setCsrfToken(null)
      queryClient.clear()
    },
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      changePassword(input.currentPassword, input.newPassword),
  })
}
