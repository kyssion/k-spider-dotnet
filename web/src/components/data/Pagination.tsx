import { Icon } from '../ui/Icon'
import { Button } from '../ui/Button'

/** 分页属性；page 从 1 开始，total 是服务端总数。 */
export interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onPageChange: (page: number) => void
}

/** 统一分页条；边界页禁用按钮，防止发出越界请求。 */
export function Pagination({ page, pageSize, total, onPageChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const currentPage = Math.min(Math.max(1, page), totalPages)

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t px-3 py-2 text-xs text-muted-foreground">
      <span>
        第 {currentPage} / {totalPages} 页，共 {total} 条
      </span>
      <div className="flex items-center gap-1">
        <Button
          variant="outline"
          size="sm"
          aria-label="上一页"
          disabled={currentPage <= 1}
          onClick={() => onPageChange(currentPage - 1)}
        >
          <Icon name="chevron-left" className="h-3.5 w-3.5" />
          上一页
        </Button>
        <Button
          variant="outline"
          size="sm"
          aria-label="下一页"
          disabled={currentPage >= totalPages}
          onClick={() => onPageChange(currentPage + 1)}
        >
          下一页
          <Icon name="chevron-right" className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  )
}
