import { useEffect } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { queryClient } from './query-client'
import { router } from './router'
import { resolveTheme } from '../lib/theme'
import { usePreferences } from '../stores/preferences'

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

export default function App() {
  useThemeEffect()

  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
