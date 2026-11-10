import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Pagination } from './Pagination'

describe('Pagination', () => {
  it('shows page summary and changes page', async () => {
    const onPageChange = vi.fn()
    render(<Pagination page={2} pageSize={10} total={31} onPageChange={onPageChange} />)

    expect(screen.getByText('第 2 / 4 页，共 31 条')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: '下一页' }))
    await userEvent.click(screen.getByRole('button', { name: '上一页' }))

    expect(onPageChange).toHaveBeenNthCalledWith(1, 3)
    expect(onPageChange).toHaveBeenNthCalledWith(2, 1)
  })

  it('disables boundaries', () => {
    render(<Pagination page={1} pageSize={10} total={10} onPageChange={() => undefined} />)
    expect(screen.getByRole('button', { name: '上一页' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '下一页' })).toBeDisabled()
  })
})
