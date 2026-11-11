import type { ReactNode } from 'react'

/** 页面标题区属性；actions 放主操作。 */
export interface PageHeaderProps {
  title: string
  description?: string
  actions?: ReactNode
}

/** 页面统一标题与操作区。 */
export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-3">
      <div className="space-y-1">
        <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        {description ? <p className="text-sm text-muted-foreground">{description}</p> : null}
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </div>
  )
}
