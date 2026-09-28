import { AlertTriangle, CheckCircle2, PauseCircle, XCircle } from 'lucide-react'
import { useFlashLag, useJobs, useNodes, usePipeline } from '../api/hooks'
import type { NodePayload } from '../api/types'
import { Badge } from '../components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { EmptyState, ErrorState, PageTitle, SectionTitle, Skeleton } from '../components/ui/states'
import { fmtDateTime, fmtDuration, lagText, NEWS_STATUS, sourceName, timeUntil } from '../lib/format'

export default function DashboardPage() {
  return (
    <div className="mx-auto max-w-6xl">
      <PageTitle title="总览" description="爬虫节点运行状态 · 管线积压 · 快讯实时性 · 反爬告警" />
      <JobSection />
      <div className="mt-8 grid gap-8 lg:grid-cols-2">
        <PipelineSection />
        <FlashLagSection />
      </div>
      <NodeSection />
    </div>
  )
}

function JobSection() {
  const { data, isPending, error } = useJobs()
  return (
    <section>
      <SectionTitle title="任务状态" />
      {isPending ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[0, 1, 2, 3, 4, 5].map((i) => (
            <Skeleton key={i} className="h-28" />
          ))}
        </div>
      ) : error ? (
        <ErrorState error={error} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {(data ?? []).map((job) => (
            <JobCard key={`${job.nodeId}/${job.jobName}`} job={job} />
          ))}
        </div>
      )}
    </section>
  )
}

function JobCard({ job }: { job: ReturnType<typeof useJobs>['data'] extends (infer T)[] | undefined ? T : never }) {
  return (
    <Card className={job.isPaused ? 'opacity-70' : undefined}>
      <CardHeader className="flex-row items-center justify-between">
        <CardTitle>{job.jobName}</CardTitle>
        {job.isPaused ? (
          <Badge variant="warning">
            <PauseCircle className="mr-1 size-3" /> 已暂停
          </Badge>
        ) : job.lastSuccess == null ? (
          <Badge variant="secondary">未执行</Badge>
        ) : job.lastSuccess ? (
          <Badge variant="success">
            <CheckCircle2 className="mr-1 size-3" /> 正常
          </Badge>
        ) : (
          <Badge variant="destructive">
            <XCircle className="mr-1 size-3" /> 失败
          </Badge>
        )}
      </CardHeader>
      <CardContent className="space-y-1 text-xs text-muted-foreground">
        <div className="flex justify-between">
          <span>下次触发</span>
          <span className="tabular-nums text-foreground">{job.isPaused ? '—' : timeUntil(job.nextFireTime)}</span>
        </div>
        <div className="flex justify-between">
          <span>上次执行</span>
          <span className="tabular-nums">{fmtDateTime(job.lastFiredAt)} · {fmtDuration(job.lastDurationMs)}</span>
        </div>
        <div className="flex justify-between gap-2">
          <span className="shrink-0">执行摘要</span>
          <span className="truncate text-right text-foreground" title={job.lastStats ?? undefined}>
            {job.lastStats ?? '—'}
          </span>
        </div>
        {job.consecutiveFailures > 0 && (
          <div className="pt-1">
            <Badge variant="destructive">连续失败 {job.consecutiveFailures} 次</Badge>
          </div>
        )}
        {job.lastError && (
          <div className="truncate pt-1 text-destructive" title={job.lastError}>
            {job.lastError.split('\n')[0]}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function PipelineSection() {
  const { data, isPending, error } = usePipeline()
  return (
    <section>
      <SectionTitle title="网页管线积压" />
      {isPending ? (
        <Skeleton className="h-40" />
      ) : error ? (
        <ErrorState error={error} />
      ) : !data?.length ? (
        <EmptyState text="暂无网页型数据" />
      ) : (
        <Card>
          <CardContent className="divide-y">
            {data.map((row) => (
              <div key={row.source} className="flex flex-wrap items-center gap-2 py-3 first:pt-0 last:pb-0">
                <span className="w-20 text-sm font-medium">{sourceName(row.source)}</span>
                {Object.entries(row.statusCounts)
                  .sort(([a], [b]) => Number(a) - Number(b))
                  .map(([code, count]) => {
                    const info = NEWS_STATUS[Number(code)]
                    return (
                      <Badge key={code} variant={(info?.tone as 'default') ?? 'outline'}>
                        {info?.label ?? code} {count}
                      </Badge>
                    )
                  })}
                {row.oldestPending && (
                  <span className="ml-auto text-xs text-muted-foreground">
                    最老待处理 {fmtDateTime(row.oldestPending)}
                  </span>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </section>
  )
}

function FlashLagSection() {
  const { data, isPending, error } = useFlashLag()
  return (
    <section>
      <SectionTitle title="快讯实时性" />
      {isPending ? (
        <Skeleton className="h-40" />
      ) : error ? (
        <ErrorState error={error} />
      ) : !data?.length ? (
        <EmptyState text="暂无快讯源" />
      ) : (
        <Card>
          <CardContent className="divide-y">
            {data.map((row) => {
              // 工作时段 ( 9~23 点 ) 滞后 5 分钟以上视为异常 , 深夜放宽到 1 小时
              const hour = new Date().getHours()
              const threshold = hour >= 9 && hour < 23 ? 300 : 3600
              const bad = row.lagSeconds != null && row.lagSeconds > threshold
              return (
                <div key={row.source} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                  <div>
                    <div className="text-sm font-medium">{sourceName(row.source)}</div>
                    <div className="text-xs text-muted-foreground tabular-nums">
                      最新一条 {fmtDateTime(row.newest)}
                    </div>
                  </div>
                  {bad ? (
                    <Badge variant="destructive">
                      <AlertTriangle className="mr-1 size-3" />
                      滞后 {lagText(row.lagSeconds)}
                    </Badge>
                  ) : (
                    <Badge variant="success">{lagText(row.lagSeconds)}</Badge>
                  )}
                </div>
              )
            })}
          </CardContent>
        </Card>
      )}
    </section>
  )
}

function NodeSection() {
  const { data, isPending, error } = useNodes()
  const staleMinutes = 6 // 快照 5 分钟一轮 , 超过 6 分钟视为节点失联
  return (
    <section className="mt-8">
      <SectionTitle title="节点与反爬状态" />
      {isPending ? (
        <Skeleton className="h-32" />
      ) : error ? (
        <ErrorState error={error} />
      ) : !data?.length ? (
        <EmptyState text="还没有节点上报 ( 主程序启动后 5 分钟内出现 )" />
      ) : (
        <div className="grid gap-4">
          {data.map((node) => (
            <NodeCard key={node.nodeId} node={node} staleMinutes={staleMinutes} />
          ))}
        </div>
      )}
    </section>
  )
}

function NodeCard({
  node,
  staleMinutes,
}: {
  node: NonNullable<ReturnType<typeof useNodes>['data']>[number]
  staleMinutes: number
}) {
  const stale =
    Date.now() - new Date(node.reportTime).getTime() > staleMinutes * 60_000
  let payload: NodePayload | null = null
  try {
    payload = node.payload ? (JSON.parse(node.payload) as NodePayload) : null
  } catch {
    payload = null
  }
  const blocked = payload?.verifyBlocked ?? []
  const apiErrors = payload?.apiAliveErrors ?? []
  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between">
        <CardTitle>{node.nodeId}</CardTitle>
        {stale ? (
          <Badge variant="destructive">失联 ( 快照 {fmtDateTime(node.reportTime)} )</Badge>
        ) : (
          <Badge variant="success">在线 · {fmtDateTime(node.reportTime)}</Badge>
        )}
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        {blocked.length === 0 && apiErrors.length === 0 && !stale ? (
          <div className="text-muted-foreground">各源接口可用 , 无反爬拦截</div>
        ) : (
          <>
            {blocked.map((block) => (
              <div
                key={block.host}
                className="flex items-start gap-2 rounded-lg border border-destructive/40 bg-destructive/5 p-3 text-xs"
              >
                <AlertTriangle className="mt-0.5 size-4 shrink-0 text-destructive" />
                <div>
                  <div className="font-medium text-destructive">
                    {block.host} 被 {block.kind} 拦截 , 冷却至 {block.until}
                  </div>
                  <div className="mt-0.5 text-muted-foreground">{block.reason}</div>
                </div>
              </div>
            ))}
            {apiErrors.map((message) => (
              <div
                key={message}
                className="rounded-lg border border-amber-500/40 bg-amber-500/5 p-3 text-xs text-amber-700 dark:text-amber-400"
              >
                接口异常 : {message}
              </div>
            ))}
          </>
        )}
      </CardContent>
    </Card>
  )
}
