import { Outlet, useNavigate } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import { LoadingState } from '../components/feedback/LoadingState'
import { useLogout, useMe } from '../features/auth/queries'

/**
 * 已登录布局：AppShell + 子路由。
 *
 * me 已由 RequireAuth 预热，这里通常直接命中缓存；登出使用 mutation 的
 * onSettled 清理本地缓存后再导航，避免残留旧查询。
 */
export function AuthenticatedLayout() {
  const me = useMe()
  const logout = useLogout()
  const navigate = useNavigate()

  if (!me.data) {
    return <LoadingState label="正在加载账号信息…" />
  }

  return (
    <AppShell
      user={me.data}
      onLogout={() => {
        logout.mutate(undefined, {
          onSettled: () => navigate('/login', { replace: true }),
        })
      }}
    >
      <Outlet />
    </AppShell>
  )
}
