import { describe, expect, it } from 'vitest'
import { nextTheme, resolveTheme, themeLabel } from './theme'

describe('theme', () => {
  it('cycles light -> dark -> system', () => {
    expect(nextTheme('light')).toBe('dark')
    expect(nextTheme('dark')).toBe('system')
    expect(nextTheme('system')).toBe('light')
  })

  it('resolves system theme', () => {
    expect(resolveTheme('system', true)).toBe('dark')
    expect(resolveTheme('system', false)).toBe('light')
    expect(resolveTheme('dark', false)).toBe('dark')
  })

  it('labels themes', () => {
    expect(themeLabel('light')).toBe('浅色')
    expect(themeLabel('dark')).toBe('深色')
    expect(themeLabel('system')).toBe('跟随系统')
  })
})
