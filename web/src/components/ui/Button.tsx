import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Icon } from './Icon'
import { cn } from '../../lib/cn'

/** 按钮视觉变体；danger 只用于不可逆操作。 */
export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'outline'
/** 按钮尺寸；icon 用于只含图标的工具按钮。 */
export type ButtonSize = 'sm' | 'md' | 'icon'

/** 原生 button 属性 + loading 态；loading 时自动禁用并设置 aria-busy。 */
export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  loading?: boolean
  children?: ReactNode
}

/** 变体到 Tailwind 类的映射，集中维护避免各页面自行拼样式。 */
const VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-primary text-primary-foreground hover:bg-primary/90 focus-visible:ring-primary/40',
  secondary:
    'bg-secondary text-secondary-foreground hover:bg-secondary/80 focus-visible:ring-ring/40',
  ghost: 'hover:bg-accent hover:text-accent-foreground focus-visible:ring-ring/40',
  danger:
    'bg-destructive text-destructive-foreground hover:bg-destructive/90 focus-visible:ring-destructive/40',
  outline:
    'border bg-background hover:bg-accent hover:text-accent-foreground focus-visible:ring-ring/40',
}

/** 尺寸到高度/内边距的映射。 */
const SIZES: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-xs',
  md: 'h-9 px-4 text-sm',
  icon: 'h-9 w-9',
}

/**
 * 基础按钮。
 *
 * 默认 type=button，避免在表单里误触提交；调用方需要提交时显式传 type="submit"。
 */
export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  className,
  children,
  disabled,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 disabled:pointer-events-none disabled:opacity-50',
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading ? <Icon name="loader" className="h-4 w-4 animate-spin" /> : null}
      {children}
    </button>
  )
}
