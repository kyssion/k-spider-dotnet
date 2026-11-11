import { useState, type FormEvent } from 'react'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { Button } from '../components/ui/Button'
import { isApiError } from '../api/errors'
import { useChangePassword } from '../features/auth/queries'

/**
 * 当前账号设置页。
 *
 * 只允许修改自己的密码；成功后不强制登出，沿用当前会话。
 */
export function ProfilePage() {
  const changePassword = useChangePassword()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  /** 修改密码；错误码映射成用户可读文案。 */
  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSuccess(false)
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setSuccess(true)
    } catch (caught) {
      setError(friendlyError(caught))
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader title="账号设置" description="修改自己的登录密码。" />
      <Card className="max-w-lg p-5">
        <form className="space-y-3" onSubmit={onSubmit}>
          <Input
            label="当前密码"
            type="password"
            autoComplete="current-password"
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
            required
          />
          <Input
            label="新密码"
            type="password"
            autoComplete="new-password"
            hint="8–128 位，允许空格与 Unicode。"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            required
          />
          {error ? <p className="text-xs text-destructive">{error}</p> : null}
          {success ? <p className="text-xs text-success">密码已更新。</p> : null}
          <Button type="submit" loading={changePassword.isPending}>
            更新密码
          </Button>
        </form>
      </Card>
    </div>
  )
}

/** 把后端错误码收敛成账号设置页可读文案。 */
function friendlyError(error: unknown): string {
  if (isApiError(error)) {
    if (error.code === 'invalid_current_password') {
      return '当前密码错误'
    }
    if (error.code === 'invalid_new_password') {
      return '新密码需为 8–128 位'
    }
    return error.message
  }
  return error instanceof Error ? error.message : '修改失败'
}
