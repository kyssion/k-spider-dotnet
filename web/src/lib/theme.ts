export type Theme = 'light' | 'dark' | 'system'

export const THEME_ORDER: Theme[] = ['light', 'dark', 'system']

export function nextTheme(theme: Theme): Theme {
  const index = THEME_ORDER.indexOf(theme)
  return THEME_ORDER[(index + 1) % THEME_ORDER.length]
}

export function resolveTheme(theme: Theme, prefersDark: boolean): 'light' | 'dark' {
  if (theme === 'system') {
    return prefersDark ? 'dark' : 'light'
  }
  return theme
}

export function themeLabel(theme: Theme): string {
  if (theme === 'light') {
    return '浅色'
  }
  if (theme === 'dark') {
    return '深色'
  }
  return '跟随系统'
}
