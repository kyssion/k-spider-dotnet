import { describe, expect, it } from 'vitest'
import { formatDateTime, formatRelative, formatUtc } from './dates'

describe('dates', () => {
  it('formats in the requested time zone', () => {
    const value = '2026-01-01T00:00:00Z'
    expect(formatDateTime(value, 'Asia/Shanghai')).toContain('08:00:00')
    expect(formatUtc(value)).toContain('00:00:00')
  })

  it('returns fallback for invalid values', () => {
    expect(formatDateTime('not-a-date', 'UTC')).toBe('—')
    expect(formatRelative(null)).toBe('—')
  })

  it('formats relative time', () => {
    const now = Date.parse('2026-01-01T00:00:00Z')
    expect(formatRelative('2025-12-31T23:59:00Z', now)).toContain('1')
  })
})
