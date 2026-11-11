/** 主题模式；system 表示跟随操作系统。 */
export type Theme = 'light' | 'dark' | 'system'

/** 主题循环顺序：浅色 → 深色 → 跟随系统。 */
export const THEME_ORDER: Theme[] = ['light', 'dark', 'system']

/**
 * 返回下一个主题。
 *
 * 只做纯计算，方便单元测试；持久化与 DOM 应用分别在 preferences / App 中。
 * @param theme 当前主题。
 * @returns 循环后的下一个主题。
 */
export function nextTheme(theme: Theme): Theme {
  const index = THEME_ORDER.indexOf(theme)
  return THEME_ORDER[(index + 1) % THEME_ORDER.length]
}

/**
 * 把主题偏好解析为实际生效的 light/dark。
 *
 * `prefersDark` 由浏览器媒体查询提供；system 模式不写入服务端，只影响展示。
 * @param theme 用户主题偏好。
 * @param prefersDark 系统是否处于深色模式。
 * @returns 实际生效的 light 或 dark。
 */
export function resolveTheme(theme: Theme, prefersDark: boolean): 'light' | 'dark' {
  if (theme === 'system') {
    return prefersDark ? 'dark' : 'light'
  }
  return theme
}

/**
 * 主题按钮与设置页共用的中文文案。
 * @param theme 主题偏好。
 * @returns 中文展示文案。
 */
export function themeLabel(theme: Theme): string {
  if (theme === 'light') {
    return '浅色'
  }
  if (theme === 'dark') {
    return '深色'
  }
  return '跟随系统'
}
