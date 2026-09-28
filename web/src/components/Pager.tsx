import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './ui/button'

interface PagerProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
}

/** 简单分页条 : 上一页 / 下一页 + 总数 */
export function Pager({ page, pageSize, total, onChange }: PagerProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  return (
    <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
      <div className="tabular-nums">
        共 {total} 条 · 第 {page} / {totalPages} 页
      </div>
      <div className="flex gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          <ChevronLeft /> 上一页
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
        >
          下一页 <ChevronRight />
        </Button>
      </div>
    </div>
  )
}
