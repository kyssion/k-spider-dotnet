import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useMe } from '../features/auth/queries'
import { isApiError } from '../api/errors'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'

export function RequireAuth() {
  const location = useLocation()
  const me = useMe()

  if (me.isLoading) {
    return (
      <div className="grid min-h-screen place-items-center">
        <LoadingState label="正在确认登录状态…" />
      </div>
    )
  }

  if (me.isError) {
    if (isApiError(me.error) && me.error.status === 401) {
      const returnUrl = `${location.pathname}${location.search}`
      return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
    }
    return (
      <div className="grid min-h-screen place-items-center">
        <ErrorState message={me.error.message} onRetry={() => void me.refetch()} />
      </div>
    )
  }

  return <Outlet />
}
