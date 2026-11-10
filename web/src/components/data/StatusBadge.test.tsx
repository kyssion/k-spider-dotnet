import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it('maps task, channel and node labels', () => {
    const { rerender } = render(<StatusBadge kind="task" status="success" />)
    expect(screen.getByText('成功')).toBeInTheDocument()

    rerender(<StatusBadge kind="channel" status="paused" />)
    expect(screen.getByText('暂停')).toBeInTheDocument()

    rerender(<StatusBadge kind="node" status="online" />)
    expect(screen.getByText('在线')).toBeInTheDocument()
  })

  it('falls back to raw status for unknown values', () => {
    render(<StatusBadge kind="task" status="mystery" />)
    expect(screen.getByText('mystery')).toBeInTheDocument()
  })
})
