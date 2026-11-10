import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Icon } from '../components/ui/Icon'
import { isApiError } from '../api/errors'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { useLogin, useMe } from '../features/auth/queries'

export function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const me = useMe()
  const login = useLogin()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const returnUrl = safeReturnUrl(searchParams.get('returnUrl'))

  useEffect(() => {
    if (me.data) {
      navigate(returnUrl, { replace: true })
    }
  }, [me.data, navigate, returnUrl])

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      await login.mutateAsync({ username, password, rememberMe })
      navigate(returnUrl, { replace: true })
    } catch (caught) {
      setError(
        isApiError(caught) && caught.status === 401
          ? '用户名或密码错误'
          : caught instanceof Error
            ? caught.message
            : '登录失败',
      )
    }
  }

  return (
    <main className="grid min-h-screen bg-background lg:grid-cols-[1.1fr_0.9fr]">
      <section className="relative hidden overflow-hidden border-r bg-slate-950 p-10 text-slate-100 lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(99,102,241,0.35),transparent_45%),radial-gradient(circle_at_80%_80%,rgba(14,165,233,0.22),transparent_40%)]" />
        <div className="relative space-y-3">
          <span className="inline-flex items-center gap-2 rounded-full border border-white/15 px-3 py-1 text-xs text-slate-300">
            KSpider · 数据平台
          </span>
          <h1 className="max-w-md text-3xl font-semibold leading-tight">
            抓取、调度与数据运营，在一个控制台里完成。
          </h1>
          <p className="max-w-md text-sm leading-6 text-slate-400">
            crawler → process → web 的单一数据链路。登录后查看节点、渠道与任务状态。
          </p>
        </div>
        <div className="relative grid grid-cols-3 gap-3 text-xs text-slate-400">
          <div className="rounded-lg border border-white/10 bg-white/5 p-3">中央调度</div>
          <div className="rounded-lg border border-white/10 bg-white/5 p-3">节点健康</div>
          <div className="rounded-lg border border-white/10 bg-white/5 p-3">原始数据</div>
        </div>
      </section>

      <section className="flex items-center justify-center p-6">
        <Card className="w-full max-w-sm p-6">
          <div className="mb-6 space-y-1">
            <div className="mb-4 grid h-10 w-10 place-items-center rounded-lg bg-primary text-sm font-bold text-primary-foreground">
              K
            </div>
            <h1 className="text-xl font-semibold">登录控制台</h1>
            <p className="text-sm text-muted-foreground">使用运维账号登录 KSpider。</p>
          </div>

          <form className="space-y-4" onSubmit={onSubmit}>
            <div className="relative">
              <Icon
                name="user"
                className="pointer-events-none absolute left-3 top-8 h-4 w-4 text-muted-foreground"
              />
              <Input
                label="用户名"
                autoComplete="username"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                className="pl-9"
                required
              />
            </div>
            <div className="relative">
              <Icon
                name="lock"
                className="pointer-events-none absolute left-3 top-8 h-4 w-4 text-muted-foreground"
              />
              <Input
                label="密码"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="pl-9"
                required
              />
            </div>

            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(event) => setRememberMe(event.target.checked)}
                className="h-4 w-4 rounded border-input accent-primary"
              />
              7 天免登录
            </label>

            {error ? (
              <p
                role="alert"
                className="rounded-md bg-destructive/10 px-3 py-2 text-xs text-destructive"
              >
                {error}
              </p>
            ) : null}

            <Button type="submit" className="w-full" loading={login.isPending}>
              登录
            </Button>
          </form>
        </Card>
      </section>
    </main>
  )
}

function safeReturnUrl(value: string | null): string {
  if (!value || !value.startsWith('/') || value.startsWith('//')) {
    return '/'
  }
  return value
}
