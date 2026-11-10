import type { ReactNode } from 'react'
import { Icon, type IconName } from '../ui/Icon'
import { Link, NavLink } from 'react-router-dom'
import type { Me } from '../../api/types'
import { themeLabel } from '../../lib/theme'
import { usePreferences } from '../../stores/preferences'
import { cn } from '../../lib/cn'
import { Button } from '../ui/Button'

const NAV_ITEMS: Array<{ to: string; label: string; icon: IconName; end: boolean }> = [
  { to: '/', label: '总览', icon: 'gauge', end: true },
  { to: '/channels', label: '渠道', icon: 'radio', end: false },
  { to: '/tasks', label: '任务', icon: 'activity', end: false },
  { to: '/nodes', label: '节点', icon: 'server', end: false },
  { to: '/settings/users', label: '账号', icon: 'users', end: false },
]

export interface AppShellProps {
  user: Pick<Me, 'username' | 'displayName'>
  onLogout: () => void
  children: ReactNode
}

export function AppShell({ user, onLogout, children }: AppShellProps) {
  const collapsed = usePreferences((state) => state.sidebarCollapsed)
  const toggleSidebar = usePreferences((state) => state.toggleSidebar)
  const theme = usePreferences((state) => state.theme)
  const cycleTheme = usePreferences((state) => state.cycleTheme)
  const timeZone = usePreferences((state) => state.timeZone)
  const setTimeZone = usePreferences((state) => state.setTimeZone)

  const themeIcon: IconName = theme === 'dark' ? 'moon' : theme === 'light' ? 'sun' : 'monitor'

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <aside
        className={cn(
          'hidden shrink-0 flex-col border-r bg-sidebar text-sidebar-foreground lg:flex',
          collapsed ? 'w-16' : 'w-60',
        )}
      >
        <div className="flex h-14 items-center gap-2 border-b px-4">
          <span className="grid h-8 w-8 place-items-center rounded-md bg-primary text-sm font-bold text-primary-foreground">
            K
          </span>
          {!collapsed ? (
            <span className="truncate text-sm font-semibold">KSpider 控制台</span>
          ) : null}
        </div>
        <nav className="flex-1 space-y-1 p-2">
          {NAV_ITEMS.map((item) => {
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                title={collapsed ? item.label : undefined}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors',
                    'hover:bg-accent hover:text-accent-foreground',
                    isActive
                      ? 'bg-accent font-medium text-accent-foreground'
                      : 'text-muted-foreground',
                    collapsed ? 'justify-center px-2' : undefined,
                  )
                }
              >
                <Icon name={item.icon} className="h-4 w-4 shrink-0" />
                {!collapsed ? <span>{item.label}</span> : null}
              </NavLink>
            )
          })}
        </nav>
        <div className="border-t p-2">
          <Button
            variant="ghost"
            size={collapsed ? 'icon' : 'sm'}
            className={collapsed ? undefined : 'w-full justify-start'}
            onClick={toggleSidebar}
            aria-label={collapsed ? '展开侧栏' : '收起侧栏'}
          >
            {collapsed ? (
              <Icon name="chevron-right" />
            ) : (
              <>
                <Icon name="chevron-left" />
                收起侧栏
              </>
            )}
          </Button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-between gap-3 border-b bg-card/80 px-4 backdrop-blur">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-muted-foreground">控制台</span>
          </div>
          <div className="flex items-center gap-1.5">
            <select
              aria-label="展示时区"
              value={timeZone}
              onChange={(event) => setTimeZone(event.target.value)}
              className="h-8 rounded-md border bg-background px-2 text-xs text-muted-foreground"
            >
              <option value="Asia/Shanghai">Asia/Shanghai</option>
              <option value="UTC">UTC</option>
              <option value="America/New_York">America/New_York</option>
            </select>
            <Button
              variant="ghost"
              size="sm"
              onClick={cycleTheme}
              aria-label={`主题：${themeLabel(theme)}`}
            >
              <Icon name={themeIcon} />
              <span className="hidden sm:inline">{themeLabel(theme)}</span>
            </Button>
            <Link
              to="/settings/profile"
              className="hidden max-w-32 truncate text-xs text-muted-foreground hover:text-foreground sm:inline"
            >
              {user.displayName ?? user.username}
            </Link>
            <Button variant="ghost" size="icon" onClick={onLogout} aria-label="退出登录">
              <Icon name="logout" />
            </Button>
          </div>
        </header>

        <nav className="flex gap-1 overflow-x-auto border-b bg-card px-3 py-2 lg:hidden">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  'whitespace-nowrap rounded-md px-2.5 py-1.5 text-xs',
                  isActive
                    ? 'bg-accent font-medium text-accent-foreground'
                    : 'text-muted-foreground',
                )
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <main className="flex-1 p-4 sm:p-6">
          <div className="mx-auto w-full max-w-[1600px] space-y-6">{children}</div>
        </main>
      </div>
    </div>
  )
}
