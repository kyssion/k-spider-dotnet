import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AppShell } from './AppShell'

describe('AppShell', () => {
  it('renders navigation, user and logout action', async () => {
    const onLogout = vi.fn()
    render(
      <MemoryRouter>
        <AppShell user={{ username: 'admin', displayName: '管理员' }} onLogout={onLogout}>
          <div>页面内容</div>
        </AppShell>
      </MemoryRouter>,
    )

    expect(screen.getAllByRole('link', { name: '渠道' }).length).toBeGreaterThan(0)
    expect(screen.getByText('管理员')).toBeInTheDocument()
    expect(screen.getByText('页面内容')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '主题：跟随系统' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: '退出登录' }))
    expect(onLogout).toHaveBeenCalledTimes(1)
  })
})
