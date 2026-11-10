import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { DataTable, type DataTableColumn } from '../components/data/DataTable'
import { Pagination } from '../components/data/Pagination'
import { StatusBadge } from '../components/data/StatusBadge'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useChannels, usePauseChannel, useResumeChannel } from '../features/channels/queries'
import { formatDateTime } from '../lib/dates'
import { usePreferences } from '../stores/preferences'
import type { ChannelView } from '../api/types'

const PAGE_SIZE = 20

export function ChannelsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const timeZone = usePreferences((state) => state.timeZone)
  const page = positiveInt(searchParams.get('page'), 1)
  const status = searchParams.get('status') ?? undefined
  const channels = useChannels({ page, pageSize: PAGE_SIZE, status })
  const pause = usePauseChannel()
  const resume = useResumeChannel()
  const [actionError, setActionError] = useState<string | null>(null)

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

  async function toggle(channel: ChannelView) {
    setActionError(null)
    try {
      if (channel.status === 'active') {
        await pause.mutateAsync(channel.id)
      } else if (channel.status === 'paused') {
        await resume.mutateAsync(channel.id)
      }
    } catch (error) {
      setActionError(error instanceof Error ? error.message : '操作失败')
    }
  }

  const columns: Array<DataTableColumn<ChannelView>> = [
    {
      key: 'name',
      header: '渠道',
      render: (channel) => (
        <Link
          to={`/channels/${channel.id}`}
          className="font-medium hover:text-primary hover:underline"
        >
          {channel.channelName}
        </Link>
      ),
    },
    {
      key: 'id',
      header: 'ID',
      render: (channel) => <span className="font-mono text-xs">#{channel.id}</span>,
    },
    {
      key: 'source',
      header: '列表适配器',
      render: (channel) => <Mono>{channel.sourceAdapter}</Mono>,
    },
    {
      key: 'detail',
      header: '详情适配器',
      render: (channel) => <Mono>{channel.detailAdapter}</Mono>,
    },
    {
      key: 'write',
      header: '写入',
      render: (channel) => <BadgeText>{channel.writeMode}</BadgeText>,
    },
    {
      key: 'retention',
      header: '保留',
      render: (channel) => (
        <span className="text-xs text-muted-foreground">
          {channel.retentionDays === null ? '永久' : `${channel.retentionDays} 天`}
        </span>
      ),
    },
    {
      key: 'status',
      header: '状态',
      render: (channel) => <StatusBadge kind="channel" status={channel.status} />,
    },
    {
      key: 'updated',
      header: '更新时间',
      render: (channel) => (
        <span className="text-xs text-muted-foreground">
          {formatDateTime(channel.updateTime, timeZone)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: '操作',
      render: (channel) =>
        channel.status === 'active' || channel.status === 'paused' ? (
          <Button
            size="sm"
            variant={channel.status === 'active' ? 'outline' : 'secondary'}
            disabled={pause.isPending || resume.isPending}
            onClick={() => void toggle(channel)}
          >
            {channel.status === 'active' ? '暂停' : '恢复'}
          </Button>
        ) : (
          <span className="text-xs text-muted-foreground">—</span>
        ),
    },
  ]

  return (
    <div className="space-y-4">
      <PageHeader
        title="渠道"
        description="管理数据渠道、适配器与调度状态。"
        actions={
          <Button variant="outline" size="sm" onClick={() => void channels.refetch()}>
            刷新
          </Button>
        }
      />

      <Card>
        <div className="flex flex-wrap items-center gap-2 border-b p-3">
          <label className="text-xs text-muted-foreground" htmlFor="channel-status">
            状态
          </label>
          <select
            id="channel-status"
            value={status ?? ''}
            onChange={(event) => updateParam('status', event.target.value || null)}
            className="h-8 rounded-md border bg-background px-2 text-xs"
          >
            <option value="">全部</option>
            <option value="active">启用</option>
            <option value="paused">暂停</option>
            <option value="retired">退役</option>
          </select>
          {actionError ? <span className="text-xs text-destructive">{actionError}</span> : null}
        </div>

        {channels.isLoading ? (
          <LoadingState />
        ) : channels.isError ? (
          <ErrorState message={channels.error.message} onRetry={() => void channels.refetch()} />
        ) : (
          <>
            <DataTable
              columns={columns}
              rows={channels.data?.items ?? []}
              rowKey={(channel) => channel.id}
              emptyTitle="没有符合条件的渠道"
              ariaLabel="渠道列表"
            />
            <Pagination
              page={channels.data?.page ?? page}
              pageSize={channels.data?.pageSize ?? PAGE_SIZE}
              total={channels.data?.total ?? 0}
              onPageChange={(nextPage) => updateParam('page', String(nextPage))}
            />
          </>
        )}
      </Card>
    </div>
  )
}

function Mono({ children }: { children: string }) {
  return <span className="font-mono text-xs text-muted-foreground">{children}</span>
}

function BadgeText({ children }: { children: string }) {
  return (
    <span className="rounded bg-muted px-1.5 py-0.5 font-mono text-[11px] text-muted-foreground">
      {children}
    </span>
  )
}

function positiveInt(value: string | null, fallback: number): number {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}
