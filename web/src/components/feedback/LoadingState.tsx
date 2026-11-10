import { Icon } from '../ui/Icon'

export interface LoadingStateProps {
  label?: string
}

export function LoadingState({ label = '加载中…' }: LoadingStateProps) {
  return (
    <div className="flex items-center justify-center gap-2 px-6 py-10 text-sm text-muted-foreground">
      <Icon name="loader" className="h-4 w-4 animate-spin" />
      <span>{label}</span>
    </div>
  )
}
