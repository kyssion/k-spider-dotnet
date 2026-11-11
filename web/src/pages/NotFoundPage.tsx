import { Link } from 'react-router-dom'
import { Icon } from '../components/ui/Icon'
import { Card } from '../components/ui/Card'

/** 404 页；路由未命中时使用，不依赖登录态。 */
export function NotFoundPage() {
  return (
    <main className="grid min-h-screen place-items-center p-6">
      <Card className="max-w-md p-8 text-center">
        <Icon name="compass" className="mx-auto h-10 w-10 text-muted-foreground" />
        <h1 className="mt-4 text-xl font-semibold">页面不存在</h1>
        <p className="mt-1 text-sm text-muted-foreground">链接可能已失效，或页面尚未开放。</p>
        <Link
          className="mt-5 inline-flex h-9 items-center justify-center rounded-md border bg-background px-4 text-sm font-medium hover:bg-accent"
          to="/"
        >
          返回总览
        </Link>
      </Card>
    </main>
  )
}
