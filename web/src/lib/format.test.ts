import { describe, expect, it } from 'vitest'
import { formatBytes, formatNumber, truncate } from './format'

describe('format', () => {
  it('formats numbers and bytes', () => {
    expect(formatNumber(1200)).toBe('1,200')
    expect(formatBytes(12)).toBe('12 B')
    expect(formatBytes(2048)).toBe('2.0 KB')
  })

  it('truncates long strings', () => {
    expect(truncate('abcdef', 3)).toBe('abc…')
    expect(truncate('abc', 3)).toBe('abc')
  })
})
