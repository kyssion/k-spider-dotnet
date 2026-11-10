import { Outlet, useNavigate } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import { LoadingState } from '../components/feedback/LoadingState'
import { useLogout, useMe } from '../features/auth/queries'

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
