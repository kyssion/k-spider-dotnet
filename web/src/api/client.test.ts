import { describe, expect, it } from 'vitest'
import { buildQuery } from './client'
import { ApiError, isApiError } from './errors'

describe('api client helpers', () => {
  it('builds query strings and skips empty values', () => {
    expect(buildQuery({ page: 1, status: undefined, channelId: null, empty: '' })).toBe('?page=1')
    expect(buildQuery({})).toBe('')
  })

  it('creates typed api errors', () => {
    const error = new ApiError(409, 'conflict', '状态已变化')
    expect(isApiError(error)).toBe(true)
    expect(error.status).toBe(409)
    expect(error.code).toBe('conflict')
  })
})
