import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { nextTheme, type Theme } from '../lib/theme'

interface PreferencesState {
  theme: Theme
  timeZone: string
  sidebarCollapsed: boolean
  setTheme: (theme: Theme) => void
  cycleTheme: () => void
  setTimeZone: (timeZone: string) => void
  toggleSidebar: () => void
}

export const usePreferences = create<PreferencesState>()(
  persist(
    (set) => ({
      theme: 'system',
      timeZone: 'Asia/Shanghai',
      sidebarCollapsed: false,
      setTheme: (theme) => set({ theme }),
      cycleTheme: () => set((state) => ({ theme: nextTheme(state.theme) })),
      setTimeZone: (timeZone) => set({ timeZone }),
      toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),
    }),
    { name: 'kspider.web.preferences' },
  ),
)
