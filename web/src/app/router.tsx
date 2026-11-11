import { createBrowserRouter } from 'react-router-dom'
import { AuthenticatedLayout } from './AuthenticatedLayout'
import { RequireAuth } from './RequireAuth'
import { LoginPage } from '../pages/LoginPage'
import { OverviewPage } from '../pages/OverviewPage'
import { ChannelsPage } from '../pages/ChannelsPage'
import { ChannelDetailPage } from '../pages/ChannelDetailPage'
import { TasksPage } from '../pages/TasksPage'
import { NodesPage } from '../pages/NodesPage'
import { UsersPage } from '../pages/UsersPage'
import { ProfilePage } from '../pages/ProfilePage'
import { NotFoundPage } from '../pages/NotFoundPage'

/** 应用路由表；登录页匿名，其余页面统一经过 RequireAuth。 */
export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AuthenticatedLayout />,
        children: [
          { index: true, element: <OverviewPage /> },
          { path: 'channels', element: <ChannelsPage /> },
          { path: 'channels/:id', element: <ChannelDetailPage /> },
          { path: 'tasks', element: <TasksPage /> },
          { path: 'nodes', element: <NodesPage /> },
          { path: 'settings/users', element: <UsersPage /> },
          { path: 'settings/profile', element: <ProfilePage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
])
