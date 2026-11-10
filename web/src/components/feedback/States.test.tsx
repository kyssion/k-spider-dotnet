import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { EmptyState } from './EmptyState'
import { ErrorState } from './ErrorState'
import { LoadingState } from './LoadingState'

describe('feedback states', () => {
  it('renders empty state with action', () => {
    render(<EmptyState title="暂无数据" description="稍后再试" action={<button>新建</button>} />)
    expect(screen.getByText('暂无数据')).toBeInTheDocument()
    expect(screen.getByText('稍后再试')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '新建' })).toBeInTheDocument()
  })

  it('renders error state and retries', async () => {
    const onRetry = vi.fn()
    render(<ErrorState message="网络错误" onRetry={onRetry} />)

    expect(screen.getByText('网络错误')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: '重试' }))
    expect(onRetry).toHaveBeenCalledTimes(1)
  })

  it('renders loading label', () => {
    render(<LoadingState label="正在加载" />)
    expect(screen.getByText('正在加载')).toBeInTheDocument()
  })
})
