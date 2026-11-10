import { useId, type InputHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
}

export function Input({ id, label, error, hint, className, ...props }: InputProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const descriptionId = error || hint ? `${inputId}-description` : undefined

  return (
    <div className="space-y-1.5">
      {label ? (
        <label htmlFor={inputId} className="text-sm font-medium text-foreground">
          {label}
        </label>
      ) : null}
      <input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={descriptionId}
        className={cn(
          'flex h-9 w-full rounded-md border bg-background px-3 py-1 text-sm shadow-sm transition-colors',
          'placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40',
          'disabled:cursor-not-allowed disabled:opacity-50',
          error ? 'border-destructive focus-visible:ring-destructive/30' : 'border-input',
          className,
        )}
        {...props}
      />
      {error ? (
        <p id={descriptionId} className="text-xs text-destructive">
          {error}
        </p>
      ) : hint ? (
        <p id={descriptionId} className="text-xs text-muted-foreground">
          {hint}
        </p>
      ) : null}
    </div>
  )
}
