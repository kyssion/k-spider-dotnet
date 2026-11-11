import { Icon } from '../ui/Icon'
import { Button } from '../ui/Button'

/** 错误态属性；onRetry 为空时不展示重试按钮。 */
export interface ErrorStateProps {
  title?: string
  message?: string
  onRetry?: () => void
}

/** 统一错误态；不展示堆栈，只展示可操作信息。 */
export function ErrorState({ title = '加载失败', message, onRetry }: ErrorStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 px-6 py-10 text-center">
      <Icon name="alert" className="h-8 w-8 text-destructive" />
      <p className="text-sm font-medium">{title}</p>
      {message ? <p className="max-w-md text-xs text-muted-foreground">{message}</p> : null}
      {onRetry ? (
        <Button variant="outline" size="sm" onClick={onRetry}>
          重试
        </Button>
      ) : null}
    </div>
  )
}
