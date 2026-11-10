import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Card } from './Card'

describe('Card', () => {
  it('renders children', () => {
    render(<Card>卡片内容</Card>)
    expect(screen.getByText('卡片内容')).toBeInTheDocument()
  })
})
