import { describe, expect, it } from 'vitest'
import { cn } from './cn'

describe('cn', () => {
  it('拼接普通类名', () => {
    expect(cn('px-2', 'text-sm')).toBe('px-2 text-sm')
  })

  it('忽略空值', () => {
    expect(cn('px-2', undefined, false, 'text-sm')).toBe('px-2 text-sm')
  })

  it('后者覆盖冲突的 Tailwind 类', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4')
  })
})
