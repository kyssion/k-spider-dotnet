import { Icon } from '../ui/Icon'
import { Button } from '../ui/Button'

export interface ErrorStateProps {
  title?: string
  message?: string
  onRetry?: () => void
}

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
