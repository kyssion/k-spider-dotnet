import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { nextTheme, type Theme } from '../lib/theme'

/**
 * 本地 UI 偏好。
 *
 * 只存展示相关状态：主题、展示时区、侧栏折叠；不复制服务端数据。
 */
interface PreferencesState {
  theme: Theme
  timeZone: string
  sidebarCollapsed: boolean
  setTheme: (theme: Theme) => void
  cycleTheme: () => void
  setTimeZone: (timeZone: string) => void
  toggleSidebar: () => void
}

/**
 * 持久化到 localStorage 的 UI 偏好 store。
 *
 * persist 只保存纯 JSON；切换时区/主题不会触发业务请求。
 */
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
