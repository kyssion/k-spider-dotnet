import type { ReactNode } from 'react'
import { Icon } from '../ui/Icon'

/** 空态属性；action 用于给用户下一步入口。 */
export interface EmptyStateProps {
  title: string
  description?: string
  action?: ReactNode
}

/** 统一空态，避免各页面出现不同的“无数据”表达。 */
export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-6 py-12 text-center">
      <Icon name="inbox" className="h-8 w-8 text-muted-foreground" />
      <p className="text-sm font-medium">{title}</p>
      {description ? <p className="max-w-md text-xs text-muted-foreground">{description}</p> : null}
      {action}
    </div>
  )
}
