import { useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Dialog } from '../components/ui/Dialog'
import { DataTable, type DataTableColumn } from '../components/data/DataTable'
import { Pagination } from '../components/data/Pagination'
import { StatusBadge } from '../components/data/StatusBadge'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useCancelTask, useTasks } from '../features/tasks/queries'
import { formatDateTime } from '../lib/dates'
import { truncate } from '../lib/format'
import { usePreferences } from '../stores/preferences'
import type { TaskView } from '../api/types'

/** 服务端分页固定页大小。 */
const PAGE_SIZE = 20
/** 终态不再提供取消入口；与后端状态机保持一致。 */
const TERMINAL = new Set(['success', 'abandoned', 'cancelled'])

/**
 * 任务列表页。
 *
 * 支持按状态筛选、分页与取消；详情/取消共用一个对话框，
 * `?task=` 用于从总览最近失败直接定位当前页任务。
 */
export function TasksPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const timeZone = usePreferences((state) => state.timeZone)
  const page = positiveInt(searchParams.get('page'), 1)
  const status = searchParams.get('status') ?? undefined
  const taskParam = searchParams.get('task')
  const tasks = useTasks({ page, pageSize: PAGE_SIZE, status })
  const cancel = useCancelTask()
  // 用户手动点开的任务；URL 指定的任务只作为派生值，二者合并为 activeTask。
  const [selected, setSelected] = useState<TaskView | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  // URL 指定的任务只在当前页数据中查找；找不到不强行打开空详情。
  const requestedTask = taskParam
    ? (tasks.data?.items.find((item) => item.id === Number(taskParam)) ?? null)
    : null
  const activeTask = selected ?? requestedTask

  /** 关闭对话框并清掉 URL 中的 task，避免刷新后重复弹出。 */
  function closeDialog() {
    setSelected(null)
    if (searchParams.has('task')) {
      const next = new URLSearchParams(searchParams)
      next.delete('task')
      setSearchParams(next)
    }
  }

  /** 更新 URL 筛选/页码；筛选变化时回到第一页，避免空页。 */
  function updateParam(key: string, value: string | null) {
    const next = new URLSearchParams(searchParams)
    if (!value) {
      next.delete(key)
    } else {
      next.set(key, value)
    }
    if (key !== 'page') {
      next.delete('page')
    }
    setSearchParams(next)
  }

  /** 二次确认后取消任务；成功后关闭并让列表重新拉取。 */
  async function confirmCancel() {
    if (!activeTask) {
      return
    }
    setActionError(null)
    try {
      await cancel.mutateAsync({ id: activeTask.id, reason: '人工取消' })
      closeDialog()
    } catch (error) {
      setActionError(error instanceof Error ? error.message : '取消失败')
    }
  }

  /** 任务列；详情/额外字段保持截断，完整内容在对话框中查看。 */
  const columns: Array<DataTableColumn<TaskView>> = [
    {
      key: 'id',
      header: '任务',
      render: (task) => <span className="font-mono text-xs">#{task.id}</span>,
    },
    {
      key: 'type',
      header: '类型',
      render: (task) => <span className="text-xs">{task.taskType}</span>,
    },
    {
      key: 'node',
      header: '节点',
      render: (task) => (
        <span className="font-mono text-[11px] text-muted-foreground">
          {task.nodeId.slice(0, 8)}
        </span>
      ),
    },
    {
      key: 'status',
      header: '状态',
      render: (task) => <StatusBadge kind="task" status={task.status} />,
    },
    {
      key: 'detail',
      header: '任务包',
      render: (task) => (
        <span
          className="block max-w-[24rem] truncate font-mono text-[11px] text-muted-foreground"
          title={task.taskDetailJson}
        >
          {truncate(task.taskDetailJson, 90)}
        </span>
      ),
    },
    {
      key: 'failure',
      header: '失败',
      render: (task) => <span className="tabular-nums text-xs">{failCount(task.extraJson)}</span>,
    },
    {
      key: 'updated',
      header: '更新时间',
      render: (task) => (
        <span className="text-xs text-muted-foreground">
          {formatDateTime(task.updateTime, timeZone)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: '操作',
      render: (task) => (
        <div className="flex gap-1.5">
          <Button variant="ghost" size="sm" onClick={() => setSelected(task)}>
            详情
          </Button>
          {TERMINAL.has(task.status) ? null : (
            <Button variant="outline" size="sm" onClick={() => setSelected(task)}>
              取消
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <PageHeader
        title="任务"
        description="查看任务状态、进度与失败原因。"
        actions={
          <Button variant="outline" size="sm" onClick={() => void tasks.refetch()}>
            刷新
          </Button>
        }
      />

      <Card>
        <div className="flex flex-wrap items-center gap-2 border-b p-3">
          <label className="text-xs text-muted-foreground" htmlFor="task-status">
            状态
          </label>
          <select
            id="task-status"
            value={status ?? ''}
            onChange={(event) => updateParam('status', event.target.value || null)}
            className="h-8 rounded-md border bg-background px-2 text-xs"
          >
            <option value="">全部</option>
            {[
              ['pushing', '推送中'],
              ['push_failed', '推送失败'],
              ['pushed', '已推送'],
              ['running', '运行中'],
              ['run_failed', '运行失败'],
              ['success', '成功'],
              ['abandoned', '已放弃'],
              ['cancelled', '已取消'],
            ].map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        {tasks.isLoading ? (
          <LoadingState />
        ) : tasks.isError ? (
          <ErrorState message={tasks.error.message} onRetry={() => void tasks.refetch()} />
        ) : (
          <>
            <DataTable
              columns={columns}
              rows={tasks.data?.items ?? []}
              rowKey={(task) => task.id}
              emptyTitle="没有符合条件的任务"
              ariaLabel="任务列表"
            />
            <Pagination
              page={tasks.data?.page ?? page}
              pageSize={tasks.data?.pageSize ?? PAGE_SIZE}
              total={tasks.data?.total ?? 0}
              onPageChange={(nextPage) => updateParam('page', String(nextPage))}
            />
          </>
        )}
      </Card>

      <Dialog
        open={activeTask !== null}
        title={activeTask ? `任务 #${activeTask.id}` : '任务详情'}
        onClose={closeDialog}
        footer={
          activeTask && !TERMINAL.has(activeTask.status) ? (
            <>
              <Button variant="ghost" onClick={closeDialog}>
                关闭
              </Button>
              <Button
                variant="danger"
                loading={cancel.isPending}
                onClick={() => void confirmCancel()}
              >
                确认取消
              </Button>
            </>
          ) : (
            <Button variant="ghost" onClick={closeDialog}>
              关闭
            </Button>
          )
        }
      >
        {activeTask ? (
          <div className="space-y-3 text-sm">
            <div className="flex items-center gap-2">
              <StatusBadge kind="task" status={activeTask.status} />
              <span className="text-xs text-muted-foreground">{activeTask.taskType}</span>
            </div>
            <Info
              label="节点"
              value={<span className="font-mono text-xs">{activeTask.nodeId}</span>}
            />
            <Info label="创建时间" value={formatDateTime(activeTask.createTime, timeZone)} />
            <Info label="更新时间" value={formatDateTime(activeTask.updateTime, timeZone)} />
            <JsonBlock title="task_detail" value={activeTask.taskDetailJson} />
            <JsonBlock title="extra" value={activeTask.extraJson} />
            {actionError ? <p className="text-xs text-destructive">{actionError}</p> : null}
          </div>
        ) : null}
      </Dialog>
    </div>
  )
}

/** 对话框字段行。 */
function Info({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-right text-xs">{value}</span>
    </div>
  )
}

/** JSON 只读块；不允许注入 HTML。 */
function JsonBlock({ title, value }: { title: string; value: string }) {
  return (
    <div>
      <p className="mb-1 text-xs font-medium">{title}</p>
      <pre className="max-h-44 overflow-auto rounded-md bg-muted p-3 font-mono text-[11px] leading-5 text-muted-foreground">
        {prettyJson(value)}
      </pre>
    </div>
  )
}

/** 美化 JSON；失败时原样展示。 */
function prettyJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value) as unknown, null, 2)
  } catch {
    return value
  }
}

/** 从 extra 解析 fail_count；脏数据回退 0。 */
function failCount(extraJson: string): number {
  try {
    const parsed = JSON.parse(extraJson) as { fail_count?: unknown }
    return typeof parsed.fail_count === 'number' ? parsed.fail_count : 0
  } catch {
    return 0
  }
}

/** 解析正整数 URL 参数；非法值回退默认页。 */
function positiveInt(value: string | null, fallback: number): number {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}
