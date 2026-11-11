import { useEffect } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { queryClient } from './query-client'
import { router } from './router'
import { resolveTheme } from '../lib/theme'
import { usePreferences } from '../stores/preferences'

/**
 * 把偏好主题应用到 <html data-theme>。
 *
 * 这是唯一写 DOM 主题的地方；system 模式监听媒体查询变化，
 * 避免组件各自读取 prefers-color-scheme 造成不一致。
 */
function useThemeEffect() {
  const theme = usePreferences((state) => state.theme)

  useEffect(() => {
    const apply = () => {
      const prefersDark =
        typeof window.matchMedia === 'function' &&
        window.matchMedia('(prefers-color-scheme: dark)').matches
      document.documentElement.dataset.theme = resolveTheme(theme, prefersDark)
    }

    apply()
    if (theme !== 'system' || typeof window.matchMedia !== 'function') {
      return
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)')
    media.addEventListener('change', apply)
    return () => media.removeEventListener('change', apply)
  }, [theme])
}

/** 根组件：QueryClient + Router + 主题副作用。 */
export default function App() {
  useThemeEffect()

  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
