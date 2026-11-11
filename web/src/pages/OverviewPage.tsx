import { Link } from 'react-router-dom'
import { Icon, type IconName } from '../components/ui/Icon'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { DataTable, type DataTableColumn } from '../components/data/DataTable'
import { StatusBadge } from '../components/data/StatusBadge'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useOverview } from '../features/overview/queries'
import { formatDateTime, formatRelative } from '../lib/dates'
import { formatNumber } from '../lib/format'
import { getTaskStatus } from '../lib/status'
import { usePreferences } from '../stores/preferences'
import type { RecentFailureView } from '../api/types'

/** 最近失败表列定义；raw JSON 不直接渲染，只展示截断后的错误。 */
const FAILURE_COLUMNS: Array<DataTableColumn<RecentFailureView>> = [
  {
    key: 'task',
    header: '任务',
    render: (row) => (
      <Link
        className="font-mono text-xs text-primary hover:underline"
        to={`/tasks?status=${row.status}`}
      >
        #{row.taskId}
      </Link>
    ),
  },
  {
    key: 'channel',
    header: '渠道',
    render: (row) => <span className="text-xs">#{row.channelId}</span>,
  },
  {
    key: 'status',
    header: '状态',
    render: (row) => <StatusBadge kind="task" status={row.status} />,
  },
  {
    key: 'fail',
    header: '失败次数',
    render: (row) => <span className="tabular-nums">{row.failCount}</span>,
  },
  {
    key: 'error',
    header: '最近错误',
    render: (row) => (
      <span
        className="block max-w-[28rem] truncate text-xs text-muted-foreground"
        title={row.lastError ?? ''}
      >
        {row.lastError ?? '—'}
      </span>
    ),
  },
  { key: 'updated', header: '更新时间', render: (row) => <RelativeTime value={row.updatedAt} /> },
]

/**
 * 总览页。
 *
 * 数据来自 /api/overview；页面只负责展示与跳转，不自行聚合多个业务接口。
 */
export function OverviewPage() {
  const overview = useOverview()
  const timeZone = usePreferences((state) => state.timeZone)

  if (overview.isLoading) {
    return <LoadingState label="正在加载总览…" />
  }

  if (overview.isError) {
    return <ErrorState message={overview.error.message} onRetry={() => void overview.refetch()} />
  }

  if (!overview.data) {
    return <LoadingState />
  }

  const { channels, tasks, nodes, recentFailures, generatedAt } = overview.data
  // 非终态是排障时最关心的积压量；终态 success/cancelled/abandoned 不计入。
  const nonTerminal =
    (tasks.byStatus.pushing ?? 0) +
    (tasks.byStatus.push_failed ?? 0) +
    (tasks.byStatus.pushed ?? 0) +
    (tasks.byStatus.running ?? 0) +
    (tasks.byStatus.run_failed ?? 0)

  return (
    <div className="space-y-6">
      <PageHeader
        title="总览"
        description="渠道、任务与节点的当前健康度。"
        actions={
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground">
              生成于 {formatDateTime(generatedAt, timeZone)}
            </span>
            <Button variant="outline" size="sm" onClick={() => void overview.refetch()}>
              刷新
            </Button>
          </div>
        }
      />

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          icon="radio"
          label="渠道"
          value={formatNumber(channels.total)}
          detail={`启用 ${channels.active} · 暂停 ${channels.paused}`}
        />
        <MetricCard
          icon="activity"
          label="非终态任务"
          value={formatNumber(nonTerminal)}
          detail={`任务总数 ${formatNumber(tasks.total)}`}
        />
        <MetricCard
          icon="server"
          label="在线节点"
          value={`${nodes.online}/${nodes.total}`}
          detail={`离线 ${nodes.offline}`}
        />
        <MetricCard
          icon="alert"
          label="最近失败"
          value={formatNumber(recentFailures.length)}
          detail="最多展示 10 条"
          tone="danger"
        />
      </section>

      <section className="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b px-4 py-3">
            <div>
              <h2 className="text-sm font-semibold">任务状态</h2>
              <p className="text-xs text-muted-foreground">按数据库当前状态统计。</p>
            </div>
            <Link
              className="text-xs font-medium text-muted-foreground hover:text-foreground"
              to="/tasks"
            >
              查看全部
            </Link>
          </div>
          <div className="grid gap-2 p-4 sm:grid-cols-2">
            {Object.entries(tasks.byStatus).map(([status, count]) => {
              const meta = getTaskStatus(status)
              return (
                <Link
                  key={status}
                  to={`/tasks?status=${status}`}
                  className="flex items-center justify-between rounded-md border px-3 py-2 hover:bg-accent"
                >
                  <StatusBadge kind="task" status={status} />
                  <span className="text-sm font-medium tabular-nums">{formatNumber(count)}</span>
                  <span className="sr-only">{meta.label}</span>
                </Link>
              )
            })}
          </div>
        </Card>

        <Card className="overflow-hidden">
          <div className="border-b px-4 py-3">
            <h2 className="text-sm font-semibold">最近失败任务</h2>
            <p className="text-xs text-muted-foreground">push_failed / run_failed / abandoned。</p>
          </div>
          <DataTable
            columns={FAILURE_COLUMNS}
            rows={recentFailures}
            rowKey={(row) => row.taskId}
            emptyTitle="暂无失败任务"
            emptyDescription="最近没有需要处理的失败或放弃任务。"
            ariaLabel="最近失败任务"
          />
        </Card>
      </section>
    </div>
  )
}

/** 总览 KPI 卡片；tone 只影响图标底色，不改变数值语义。 */
function MetricCard({
  icon,
  label,
  value,
  detail,
  tone = 'default',
}: {
  icon: IconName
  label: string
  value: string
  detail: string
  tone?: 'default' | 'danger'
}) {
  return (
    <Card className="p-4">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-muted-foreground">{label}</p>
          <p className="mt-1 text-2xl font-semibold tabular-nums">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{detail}</p>
        </div>
        <span
          className={
            tone === 'danger'
              ? 'grid h-9 w-9 place-items-center rounded-md bg-destructive/10 text-destructive'
              : 'grid h-9 w-9 place-items-center rounded-md bg-primary/10 text-primary'
          }
        >
          <Icon name={icon} />
        </span>
      </div>
    </Card>
  )
}

/** 列表相对时间；hover 仍能看到原始值。 */
function RelativeTime({ value }: { value: string }) {
  return (
    <span title={value} className="text-xs text-muted-foreground">
      {formatRelative(value)}
    </span>
  )
}
