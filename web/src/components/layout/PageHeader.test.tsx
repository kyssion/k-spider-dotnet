import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PageHeader } from './PageHeader'

describe('PageHeader', () => {
  it('renders title, description and actions', () => {
    render(<PageHeader title="渠道" description="管理渠道" actions={<button>新建</button>} />)
    expect(screen.getByRole('heading', { name: '渠道' })).toBeInTheDocument()
    expect(screen.getByText('管理渠道')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '新建' })).toBeInTheDocument()
  })
})
