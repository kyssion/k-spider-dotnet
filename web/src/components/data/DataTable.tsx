import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'
import { EmptyState } from '../feedback/EmptyState'
import { LoadingState } from '../feedback/LoadingState'

/** 列定义；render 负责单元格展示，className 控制列宽/对齐。 */
export interface DataTableColumn<T> {
  key: string
  header: ReactNode
  render: (row: T) => ReactNode
  className?: string
}

/** 表格属性；loading/empty 由组件统一呈现，页面只提供数据。 */
export interface DataTableProps<T> {
  columns: Array<DataTableColumn<T>>
  rows: T[]
  rowKey: (row: T) => string | number
  loading?: boolean
  emptyTitle?: string
  emptyDescription?: string
  onRowClick?: (row: T) => void
  ariaLabel?: string
}

/**
 * 轻量数据表格。
 *
 * 只做展示与行点击，不做排序/虚拟滚动；服务端分页保证单页数据量可控。
 * 行可点击时补 tabIndex 与 Enter/Space 键盘处理，避免只能用鼠标。
 */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  emptyTitle = '暂无数据',
  emptyDescription,
  onRowClick,
  ariaLabel,
}: DataTableProps<T>) {
  if (loading) {
    return <LoadingState />
  }

  if (rows.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} />
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm" aria-label={ariaLabel}>
        <thead className="sticky top-0 z-10 bg-muted/80 text-xs uppercase text-muted-foreground backdrop-blur">
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={cn('whitespace-nowrap px-3 py-2 font-medium', column.className)}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              className={cn(
                'border-t transition-colors',
                onRowClick ? 'cursor-pointer hover:bg-accent/60 focus:bg-accent/60' : undefined,
              )}
              tabIndex={onRowClick ? 0 : undefined}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              onKeyDown={
                onRowClick
                  ? (event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault()
                        onRowClick(row)
                      }
                    }
                  : undefined
              }
            >
              {columns.map((column) => (
                <td
                  key={column.key}
                  className={cn('whitespace-nowrap px-3 py-2.5', column.className)}
                >
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
