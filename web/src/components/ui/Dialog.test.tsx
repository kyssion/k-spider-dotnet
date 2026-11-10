import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Dialog } from './Dialog'

describe('Dialog', () => {
  it('renders when open and closes from the close button', async () => {
    const onClose = vi.fn()
    render(
      <Dialog open title="确认" onClose={onClose}>
        内容
      </Dialog>,
    )

    expect(screen.getByRole('dialog', { name: '确认' })).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: '关闭' }))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('closes on Escape', () => {
    const onClose = vi.fn()
    render(
      <Dialog open title="确认" onClose={onClose}>
        内容
      </Dialog>,
    )

    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('renders nothing when closed', () => {
    render(
      <Dialog open={false} title="确认" onClose={() => undefined}>
        内容
      </Dialog>,
    )
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
