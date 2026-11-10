import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Badge } from './Badge'

describe('Badge', () => {
  it('renders tone style and text', () => {
    render(<Badge tone="success">成功</Badge>)
    const badge = screen.getByText('成功')
    expect(badge).toHaveClass('text-success')
  })
})
