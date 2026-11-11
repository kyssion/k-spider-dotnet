import { Icon } from '../ui/Icon'

/** 加载态属性；label 用于区分页面级与局部加载。 */
export interface LoadingStateProps {
  label?: string
}

/** 统一加载态。 */
export function LoadingState({ label = '加载中…' }: LoadingStateProps) {
  return (
    <div className="flex items-center justify-center gap-2 px-6 py-10 text-sm text-muted-foreground">
      <Icon name="loader" className="h-4 w-4 animate-spin" />
      <span>{label}</span>
    </div>
  )
}
