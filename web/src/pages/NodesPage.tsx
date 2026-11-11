import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { DataTable, type DataTableColumn } from '../components/data/DataTable'
import { StatusBadge } from '../components/data/StatusBadge'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useNodes } from '../features/nodes/queries'
import { formatDateTime } from '../lib/dates'
import { usePreferences } from '../stores/preferences'
import type { NodeView } from '../api/types'

/**
 * 节点状态页。
 *
 * 展示 sys_node 镜像；实时在线判定由 admin 内存心跳负责，页面不推断。
 */
export function NodesPage() {
  const nodes = useNodes()
  const timeZone = usePreferences((state) => state.timeZone)

  /** 节点列；device 只做截断展示，完整 JSON 通过 title 查看。 */
  const columns: Array<DataTableColumn<NodeView>> = [
    {
      key: 'id',
      header: '节点',
      render: (node) => <span className="font-mono text-xs">{node.nodeId.slice(0, 12)}…</span>,
    },
    {
      key: 'type',
      header: '类型',
      render: (node) => <span className="text-xs">{node.nodeType}</span>,
    },
    {
      key: 'status',
      header: '状态',
      render: (node) => <StatusBadge kind="node" status={node.status} />,
    },
    {
      key: 'device',
      header: '设备',
      render: (node) => (
        <span
          className="block max-w-[22rem] truncate font-mono text-[11px] text-muted-foreground"
          title={node.deviceJson ?? ''}
        >
          {node.deviceJson ?? '—'}
        </span>
      ),
    },
    {
      key: 'updated',
      header: '镜像更新时间',
      render: (node) => (
        <span className="text-xs text-muted-foreground">
          {formatDateTime(node.updateTime, timeZone)}
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <PageHeader
        title="节点"
        description="节点镜像与在线状态；实时判定由 admin 内存心跳负责。"
        actions={
          <Button variant="outline" size="sm" onClick={() => void nodes.refetch()}>
            刷新
          </Button>
        }
      />
      <Card>
        {nodes.isLoading ? (
          <LoadingState />
        ) : nodes.isError ? (
          <ErrorState message={nodes.error.message} onRetry={() => void nodes.refetch()} />
        ) : (
          <DataTable
            columns={columns}
            rows={nodes.data ?? []}
            rowKey={(node) => node.nodeId}
            emptyTitle="暂无节点"
            emptyDescription="crawler / process 节点启动并连接 admin 后会出现在这里。"
            ariaLabel="节点列表"
          />
        )}
      </Card>
    </div>
  )
}
