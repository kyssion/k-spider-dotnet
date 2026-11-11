import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { changePassword, getMe, login, logout } from '../../api/endpoints/auth'
import { setCsrfToken } from '../../api/client'
import type { LoginInput } from '../../api/types'
import { queryKeys } from '../../lib/query-keys'

/**
 * 当前登录用户。
 *
 * retry=false：401 是预期状态，路由守卫据此跳转，不需要重试。
 */
export function useMe() {
  return useQuery({
    queryKey: queryKeys.auth.me,
    queryFn: getMe,
    retry: false,
    staleTime: 30_000,
  })
}

/** 登录 mutation；成功后失效 me，让守卫重新读取会话。 */
export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: LoginInput) => login(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me })
    },
  })
}

/** 登出 mutation；无论后端是否成功都清空本地会话缓存。 */
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

/** 修改自己的密码；不清理当前会话，用户仍可继续操作。 */
export function useChangePassword() {
  return useMutation({
    mutationFn: (input: { currentPassword: string; newPassword: string }) =>
      changePassword(input.currentPassword, input.newPassword),
  })
}
