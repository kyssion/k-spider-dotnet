import * as React from 'react'
import { X } from 'lucide-react'
import { cn } from '../../lib/utils'

interface DialogProps {
  open: boolean
  onClose: () => void
  title: React.ReactNode
  children: React.ReactNode
  className?: string
}

/** 轻量对话框 ( 点击遮罩 / Esc 关闭 ) */
export function Dialog({ open, onClose, title, children, className }: DialogProps) {
  React.useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
      <div className="absolute inset-0 bg-black/50" onClick={onClose} />
      <div
        role="dialog"
        aria-modal="true"
        className={cn(
          'relative flex max-h-[85vh] w-full max-w-3xl flex-col rounded-xl border bg-card shadow-lg',
          className,
        )}
      >
        <div className="flex items-center justify-between border-b px-5 py-4">
          <div className="min-w-0 text-sm font-semibold">{title}</div>
          <button
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
            onClick={onClose}
          >
            <X className="size-4" />
          </button>
        </div>
        <div className="min-h-0 flex-1 overflow-auto p-5">{children}</div>
      </div>
    </div>
  )
}
