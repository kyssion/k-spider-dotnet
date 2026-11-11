import type { HTMLAttributes, ReactNode } from 'react'
import { cn } from '../../lib/cn'

/** 卡片容器；页面统一使用细边框与轻阴影，不另建样式体系。 */
export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

/** 基础卡片。 */
export function Card({ className, children, ...props }: CardProps) {
  return (
    <div
      className={cn('rounded-lg border bg-card text-card-foreground shadow-sm', className)}
      {...props}
    >
      {children}
    </div>
  )
}
