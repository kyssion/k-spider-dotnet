import { useState, type FormEvent } from 'react'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Dialog } from '../components/ui/Dialog'
import { Input } from '../components/ui/Input'
import { Badge } from '../components/ui/Badge'
import { DataTable, type DataTableColumn } from '../components/data/DataTable'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useMe } from '../features/auth/queries'
import { useCreateUser, usePatchUser, useResetPassword, useUsers } from '../features/users/queries'
import { formatDateTime } from '../lib/dates'
import { usePreferences } from '../stores/preferences'
import type { UserView } from '../api/types'

export function UsersPage() {
  const users = useUsers()
  const me = useMe()
  const create = useCreateUser()
  const patch = usePatchUser()
  const reset = useResetPassword()
  const timeZone = usePreferences((state) => state.timeZone)

  const [createOpen, setCreateOpen] = useState(false)
  const [resetTarget, setResetTarget] = useState<UserView | null>(null)
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function onCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      await create.mutateAsync({
        username,
        password,
        displayName: displayName.trim() || null,
      })
      setUsername('')
      setPassword('')
      setDisplayName('')
      setCreateOpen(false)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : '创建失败')
    }
  }

  async function onReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!resetTarget) {
      return
    }
    setError(null)
    try {
      await reset.mutateAsync({ id: resetTarget.id, newPassword })
      setNewPassword('')
      setResetTarget(null)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : '重置失败')
    }
  }

  async function toggleStatus(user: UserView) {
    setError(null)
    try {
      await patch.mutateAsync({
        id: user.id,
        patch: { status: user.status === 'active' ? 'disabled' : 'active' },
      })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : '操作失败')
    }
  }

  const columns: Array<DataTableColumn<UserView>> = [
    {
      key: 'username',
      header: '用户名',
      render: (user) => <span className="font-medium">{user.username}</span>,
    },
    {
      key: 'display',
      header: '展示名',
      render: (user) => <span className="text-xs">{user.displayName ?? '—'}</span>,
    },
    {
      key: 'status',
      header: '状态',
      render: (user) => (
        <Badge tone={user.status === 'active' ? 'success' : 'neutral'}>
          {user.status === 'active' ? '启用' : '禁用'}
        </Badge>
      ),
    },
    {
      key: 'created',
      header: '创建时间',
      render: (user) => (
        <span className="text-xs text-muted-foreground">
          {formatDateTime(user.createTime, timeZone)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: '操作',
      render: (user) => (
        <div className="flex gap-1.5">
          <Button
            size="sm"
            variant="outline"
            disabled={patch.isPending || user.id === me.data?.id}
            onClick={() => void toggleStatus(user)}
          >
            {user.status === 'active' ? '禁用' : '启用'}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            disabled={user.id === me.data?.id}
            onClick={() => {
              setError(null)
              setNewPassword('')
              setResetTarget(user)
            }}
          >
            重置密码
          </Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <PageHeader
        title="账号"
        description="当前所有登录用户同权；账号创建与密码重置写应用日志。"
        actions={
          <Button
            size="sm"
            onClick={() => {
              setError(null)
              setCreateOpen(true)
            }}
          >
            新建账号
          </Button>
        }
      />

      <Card>
        {error ? (
          <p className="border-b bg-destructive/10 px-4 py-2 text-xs text-destructive">{error}</p>
        ) : null}
        {users.isLoading ? (
          <LoadingState />
        ) : users.isError ? (
          <ErrorState message={users.error.message} onRetry={() => void users.refetch()} />
        ) : (
          <DataTable
            columns={columns}
            rows={users.data ?? []}
            rowKey={(user) => user.id}
            emptyTitle="暂无账号"
            ariaLabel="账号列表"
          />
        )}
      </Card>

      <Dialog
        open={createOpen}
        title="新建账号"
        onClose={() => setCreateOpen(false)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setCreateOpen(false)}>
              取消
            </Button>
            <Button type="submit" form="create-user-form" loading={create.isPending}>
              创建
            </Button>
          </>
        }
      >
        <form id="create-user-form" className="space-y-3" onSubmit={onCreate}>
          <Input
            label="用户名"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
          />
          <Input
            label="初始密码"
            type="password"
            hint="8–128 位，允许空格与 Unicode。"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          <Input
            label="展示名"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
          />
          {error ? <p className="text-xs text-destructive">{error}</p> : null}
        </form>
      </Dialog>

      <Dialog
        open={resetTarget !== null}
        title={resetTarget ? `重置 ${resetTarget.username} 的密码` : '重置密码'}
        onClose={() => setResetTarget(null)}
        footer={
          <>
            <Button variant="ghost" onClick={() => setResetTarget(null)}>
              取消
            </Button>
            <Button type="submit" form="reset-password-form" loading={reset.isPending}>
              重置
            </Button>
          </>
        }
      >
        <form id="reset-password-form" className="space-y-3" onSubmit={onReset}>
          <Input
            label="新密码"
            type="password"
            hint="8–128 位。"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            required
          />
          {error ? <p className="text-xs text-destructive">{error}</p> : null}
        </form>
      </Dialog>
    </div>
  )
}
