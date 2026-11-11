import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'
import type { StatusTone } from '../../lib/status'

/** 状态徽标；tone 只表达语义，具体颜色由 TONES 统一维护。 */
export interface BadgeProps {
  tone?: StatusTone
  children: ReactNode
  className?: string
}

/** 语义色映射；状态文字与色点同时存在，不依赖颜色单独传达信息。 */
const TONES: Record<StatusTone, string> = {
  neutral: 'bg-muted text-muted-foreground ring-border',
  info: 'bg-info/10 text-info ring-info/20',
  success: 'bg-success/10 text-success ring-success/20',
  warning: 'bg-warning/10 text-warning ring-warning/20',
  danger: 'bg-destructive/10 text-destructive ring-destructive/20',
  running: 'bg-primary/10 text-primary ring-primary/20',
}

/** 轻量状态徽标。 */
export function Badge({ tone = 'neutral', children, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset',
        TONES[tone],
        className,
      )}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true" />
      {children}
    </span>
  )
}
