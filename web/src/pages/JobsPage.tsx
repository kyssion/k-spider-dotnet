import * as React from 'react'
import { Pause, Play, RotateCcw } from 'lucide-react'
import { useCommands, useJobs, useSendCommand } from '../api/hooks'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { EmptyState, ErrorState, PageTitle, SectionTitle, Skeleton } from '../components/ui/states'
import { Table, TBody, TD, TH, THead, TR } from '../components/ui/table'
import { fmtDateTime, fmtDuration, timeUntil } from '../lib/format'

const ACTION_LABELS: Record<string, string> = {
  trigger: '触发',
  pause: '暂停',
  resume: '恢复',
}

const COMMAND_STATUS: Record<string, { label: string; tone: string }> = {
  pending: { label: '待消费', tone: 'warning' },
  done: { label: '已执行', tone: 'success' },
  rejected: { label: '已拒绝', tone: 'destructive' },
}

export default function JobsPage() {
  return (
    <div className="mx-auto max-w-6xl">
      <PageTitle
        title="任务管理"
        description="手动触发 / 暂停 / 恢复 ( 指令约 3 秒内被爬虫节点消费 )"
      />
      <JobsTable />
      <section className="mt-8">
        <SectionTitle title="指令历史" />
        <CommandsTable />
      </section>
    </div>
  )
}

function JobsTable() {
  const { data, isPending, error } = useJobs()
  const sendCommand = useSendCommand()
  const [confirming, setConfirming] = React.useState<string | null>(null)

  const send = (jobName: string, action: string) => {
    sendCommand.mutate({ jobName, action })
    setConfirming(null)
  }

  return (
    <Card>
      <CardContent className="pt-5">
        {isPending ? (
          <Skeleton className="h-64" />
        ) : error ? (
          <ErrorState error={error} />
        ) : !data?.length ? (
          <EmptyState text="没有任务状态 ( 主程序启动后出现 )" />
        ) : (
          <Table>
            <THead>
              <TR>
                <TH>任务</TH>
                <TH>调度</TH>
                <TH>上次执行</TH>
                <TH>摘要</TH>
                <TH className="text-right">操作</TH>
              </TR>
            </THead>
            <TBody>
              {data.map((job) => (
                <TR key={`${job.nodeId}/${job.jobName}`}>
                  <TD className="font-medium whitespace-nowrap">
                    {job.jobName}
                    {job.isPaused && <Badge variant="warning" className="ml-2">已暂停</Badge>}
                    {job.consecutiveFailures > 0 && (
                      <Badge variant="destructive" className="ml-2">
                        连续失败 {job.consecutiveFailures}
                      </Badge>
                    )}
                  </TD>
                  <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                    {job.isPaused ? '—' : timeUntil(job.nextFireTime)}
                  </TD>
                  <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                    {fmtDateTime(job.lastFiredAt)}
                    <span className="ml-1">({fmtDuration(job.lastDurationMs)})</span>
                  </TD>
                  <TD className="max-w-52 truncate text-muted-foreground" title={job.lastStats ?? ''}>
                    {job.lastStats ?? '—'}
                  </TD>
                  <TD className="text-right whitespace-nowrap">
                    {confirming === `pause:${job.jobName}` ? (
                      <span className="inline-flex gap-1">
                        <Button size="sm" variant="destructive" onClick={() => send(job.jobName, 'pause')}>
                          确认暂停
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => setConfirming(null)}>
                          取消
                        </Button>
                      </span>
                    ) : (
                      <span className="inline-flex gap-1">
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={sendCommand.isPending}
                          onClick={() => send(job.jobName, 'trigger')}
                        >
                          <Play /> 触发
                        </Button>
                        {job.isPaused ? (
                          <Button
                            size="sm"
                            variant="outline"
                            disabled={sendCommand.isPending}
                            onClick={() => send(job.jobName, 'resume')}
                          >
                            <RotateCcw /> 恢复
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="ghost"
                            disabled={sendCommand.isPending}
                            onClick={() => setConfirming(`pause:${job.jobName}`)}
                          >
                            <Pause /> 暂停
                          </Button>
                        )}
                      </span>
                    )}
                  </TD>
                </TR>
              ))}
            </TBody>
          </Table>
        )}
        {sendCommand.isError && (
          <div className="mt-3 text-xs text-destructive">
            指令发送失败 : {sendCommand.error.message}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function CommandsTable() {
  const { data, isPending, error } = useCommands()
  return (
    <Card>
      <CardContent className="pt-5">
        {isPending ? (
          <Skeleton className="h-48" />
        ) : error ? (
          <ErrorState error={error} />
        ) : !data?.length ? (
          <EmptyState text="还没有指令记录" />
        ) : (
          <Table>
            <THead>
              <TR>
                <TH>ID</TH>
                <TH>任务</TH>
                <TH>指令</TH>
                <TH>状态</TH>
                <TH>结果</TH>
                <TH>下发时间</TH>
                <TH>消费时间</TH>
              </TR>
            </THead>
            <TBody>
              {data.map((command) => {
                const status = COMMAND_STATUS[command.status]
                return (
                  <TR key={command.id}>
                    <TD className="tabular-nums text-muted-foreground">{command.id}</TD>
                    <TD className="whitespace-nowrap">{command.jobName}</TD>
                    <TD>{ACTION_LABELS[command.action] ?? command.action}</TD>
                    <TD>
                      <Badge variant={(status?.tone as 'default') ?? 'secondary'}>
                        {status?.label ?? command.status}
                      </Badge>
                    </TD>
                    <TD className="max-w-64 truncate text-muted-foreground" title={command.result ?? ''}>
                      {command.result ?? '—'}
                    </TD>
                    <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                      {fmtDateTime(command.createTime)}
                    </TD>
                    <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                      {fmtDateTime(command.consumedAt)}
                    </TD>
                  </TR>
                )
              })}
            </TBody>
          </Table>
        )}
      </CardContent>
    </Card>
  )
}
