import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Input } from './Input'

describe('Input', () => {
  it('associates label with input', () => {
    render(<Input label="用户名" />)
    expect(screen.getByLabelText('用户名')).toBeInTheDocument()
  })

  it('shows error and sets aria-invalid', () => {
    render(<Input label="用户名" error="用户名已存在" />)
    const input = screen.getByLabelText('用户名')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('用户名已存在')).toBeInTheDocument()
  })

  it('shows hint when there is no error', () => {
    render(<Input label="密码" hint="至少 8 位" />)
    expect(screen.getByText('至少 8 位')).toBeInTheDocument()
  })
})
