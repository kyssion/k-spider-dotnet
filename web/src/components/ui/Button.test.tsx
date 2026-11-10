import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Button } from './Button'

describe('Button', () => {
  it('renders text and handles click', async () => {
    const onClick = vi.fn()
    render(<Button onClick={onClick}>保存</Button>)

    await userEvent.click(screen.getByRole('button', { name: '保存' }))

    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('disables and marks busy while loading', () => {
    render(<Button loading>保存</Button>)

    const button = screen.getByRole('button', { name: '保存' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('keeps explicit submit type', () => {
    render(<Button type="submit">提交</Button>)
    expect(screen.getByRole('button', { name: '提交' })).toHaveAttribute('type', 'submit')
  })
})
