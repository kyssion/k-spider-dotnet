import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Icon } from '../components/ui/Icon'
import { PageHeader } from '../components/layout/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { StatusBadge } from '../components/data/StatusBadge'
import { ErrorState } from '../components/feedback/ErrorState'
import { LoadingState } from '../components/feedback/LoadingState'
import { useChannel, usePauseChannel, useResumeChannel } from '../features/channels/queries'
import { formatDateTime } from '../lib/dates'
import { usePreferences } from '../stores/preferences'

export function ChannelDetailPage() {
  const params = useParams()
  const id = Number(params.id)
  const channel = useChannel(id)
  const pause = usePauseChannel()
  const resume = useResumeChannel()
  const timeZone = usePreferences((state) => state.timeZone)
  const [actionError, setActionError] = useState<string | null>(null)

  if (channel.isLoading) {
    return <LoadingState label="正在加载渠道…" />
  }
  if (channel.isError) {
    return <ErrorState message={channel.error.message} onRetry={() => void channel.refetch()} />
  }
  if (!channel.data) {
    return <ErrorState title="渠道不存在" message="该渠道可能已被删除。" />
  }

  const data = channel.data
  const busy = pause.isPending || resume.isPending

  async function toggle() {
    setActionError(null)
    try {
      if (data.status === 'active') {
        await pause.mutateAsync(data.id)
      } else if (data.status === 'paused') {
        await resume.mutateAsync(data.id)
      }
    } catch (caught) {
      setActionError(caught instanceof Error ? caught.message : '操作失败')
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title={data.channelName}
        description={`渠道 #${data.id}`}
        actions={
          <div className="flex items-center gap-2">
            <Link
              className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
              to="/channels"
            >
              <Icon name="arrow-left" className="h-3.5 w-3.5" />
              返回列表
            </Link>
            {data.status === 'active' ? (
              <Button size="sm" variant="outline" disabled={busy} onClick={() => void toggle()}>
                暂停
              </Button>
            ) : data.status === 'paused' ? (
              <Button size="sm" variant="secondary" disabled={busy} onClick={() => void toggle()}>
                恢复
              </Button>
            ) : null}
          </div>
        }
      />

      {actionError ? <p className="text-xs text-destructive">{actionError}</p> : null}

      <div className="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
        <Card className="p-4">
          <h2 className="text-sm font-semibold">基本信息</h2>
          <dl className="mt-3 space-y-3 text-sm">
            <Info label="状态" value={<StatusBadge kind="channel" status={data.status} />} />
            <Info label="列表适配器" value={<Mono>{data.sourceAdapter}</Mono>} />
            <Info label="详情适配器" value={<Mono>{data.detailAdapter}</Mono>} />
            <Info label="写入模式" value={<Mono>{data.writeMode}</Mono>} />
            <Info
              label="保留期"
              value={data.retentionDays === null ? '永久' : `${data.retentionDays} 天`}
            />
            <Info label="创建时间" value={formatDateTime(data.createTime, timeZone)} />
            <Info label="更新时间" value={formatDateTime(data.updateTime, timeZone)} />
          </dl>
        </Card>

        <div className="space-y-4">
          <JsonCard title="schedule" value={data.scheduleJson} />
          <JsonCard title="extra" value={data.extraJson} />
        </div>
      </div>
    </div>
  )
}

function Info({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-right text-xs">{value}</dd>
    </div>
  )
}

function Mono({ children }: { children: string }) {
  return <span className="font-mono text-xs">{children}</span>
}

function JsonCard({ title, value }: { title: string; value: string }) {
  return (
    <Card className="overflow-hidden">
      <div className="border-b px-4 py-3 text-sm font-semibold">{title}</div>
      <pre className="max-h-80 overflow-auto p-4 font-mono text-xs leading-5 text-muted-foreground">
        {prettyJson(value)}
      </pre>
    </Card>
  )
}

function prettyJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value) as unknown, null, 2)
  } catch {
    return value
  }
}
