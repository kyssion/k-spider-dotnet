import { describe, expect, it } from 'vitest'
import { getChannelStatus, getNodeStatus, getTaskStatus } from './status'

describe('status mapping', () => {
  it('maps known statuses', () => {
    expect(getTaskStatus('running')).toEqual({ label: '运行中', tone: 'running' })
    expect(getChannelStatus('paused')).toEqual({ label: '暂停', tone: 'warning' })
    expect(getNodeStatus('offline')).toEqual({ label: '离线', tone: 'neutral' })
  })

  it('falls back to raw value', () => {
    expect(getTaskStatus('unknown')).toEqual({ label: 'unknown', tone: 'neutral' })
  })
})
