import { QueryClient } from '@tanstack/react-query'
import { isApiError } from '../api/errors'

/**
 * 全局 QueryClient。
 *
 * 4xx 不重试，避免把鉴权/冲突错误反复打到后端；5xx 最多重试两次。
 * mutation 默认不重试，防止重复写。
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (isApiError(error) && error.status < 500) {
          return false
        }
        return failureCount < 2
      },
    },
    mutations: {
      retry: false,
    },
  },
})
