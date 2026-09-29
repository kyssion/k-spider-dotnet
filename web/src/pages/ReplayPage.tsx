import * as React from 'react'
import { Eye, Play } from 'lucide-react'
import {
  useReplayParsers,
  useReplayPreview,
  useReplayTasks,
  useSubmitReplay,
} from '../api/hooks'
import type { ReplayTask } from '../api/types'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card, CardContent } from '../components/ui/card'
import { Input, Select } from '../components/ui/input'
import { EmptyState, ErrorState, PageTitle, SectionTitle, Skeleton } from '../components/ui/states'
import { Table, TBody, TD, TH, THead, TR } from '../components/ui/table'
import { fmtDateTime, sourceName } from '../lib/format'

const TASK_STATUS: Record<string, { label: string; tone: string }> = {
  running: { label: '运行中', tone: 'warning' },
  done: { label: '完成', tone: 'success' },
  failed: { label: '失败', tone: 'destructive' },
}

/** 任务的筛选条件 JSON → 摘要文案 */
function filterSummary(task: ReplayTask): string {
  try {
    const filter = JSON.parse(task.filter ?? '{}') as {
      fromMedia?: number | null
      parserCode?: string | null
      maxCount?: number
    }
    const parts: string[] = []
    if (filter.parserCode) parts.push(filter.parserCode)
    else if (filter.fromMedia) parts.push(sourceName(filter.fromMedia))
    else parts.push('全部来源')
    if (filter.maxCount) parts.push(`上限 ${filter.maxCount}`)
    return parts.join(' · ')
  } catch {
    return task.filter ?? ''
  }
}

export default function ReplayPage() {
  const [fromMedia, setFromMedia] = React.useState('')
  const [parserCode, setParserCode] = React.useState('')
  const [start, setStart] = React.useState('')
  const [end, setEnd] = React.useState('')
  const [maxCount, setMaxCount] = React.useState('20000')

  const parsers = useReplayParsers()
  const preview = useReplayPreview()
  const submit = useSubmitReplay()
  const tasks = useReplayTasks()

  const buildParams = () => ({
    fromMedia: fromMedia ? Number(fromMedia) : null,
    parserCode: parserCode || null,
    start: start || null,
    end: end || null,
    maxCount: Number(maxCount) || 20000,
  })

  return (
    <div className="mx-auto max-w-5xl">
      <PageTitle
        title="数据重放"
        description="把已下载的原始内容 ( origin ) 按解析器重新生成为结构化正文——网页零重抓 , 用于解析升级后的数据修复"
      />

      <Card className="mb-6">
        <CardContent className="grid gap-3 pt-5 sm:grid-cols-2 lg:grid-cols-3">
          <label className="grid gap-1 text-xs text-muted-foreground">
            来源
            <Select aria-label="来源" value={fromMedia} onChange={(e) => setFromMedia(e.target.value)}>
              <option value="">全部来源</option>
              {(parsers.data ?? []).map((parser) => (
                <option key={parser.code} value={parser.fromMedia}>
                  {sourceName(parser.fromMedia)}
                </option>
              ))}
            </Select>
          </label>
          <label className="grid gap-1 text-xs text-muted-foreground">
            解析器
            <Select aria-label="解析器" value={parserCode} onChange={(e) => setParserCode(e.target.value)}>
              <option value="">全部解析器</option>
              {(parsers.data ?? []).map((parser) => (
                <option key={parser.code} value={parser.code}>
                  {parser.code}
                </option>
              ))}
            </Select>
          </label>
          <label className="grid gap-1 text-xs text-muted-foreground">
            条数上限
            <Input
              aria-label="条数上限"
              type="number"
              min={1}
              value={maxCount}
              onChange={(e) => setMaxCount(e.target.value)}
            />
          </label>
          <label className="grid gap-1 text-xs text-muted-foreground">
            入库时间起
            <Input aria-label="入库时间起" type="date" value={start} onChange={(e) => setStart(e.target.value)} />
          </label>
          <label className="grid gap-1 text-xs text-muted-foreground">
            入库时间止
            <Input aria-label="入库时间止" type="date" value={end} onChange={(e) => setEnd(e.target.value)} />
          </label>
          <div className="flex items-end gap-2">
            <Button
              variant="outline"
              disabled={preview.isPending}
              onClick={() => preview.mutate(buildParams())}
            >
              <Eye /> 预览命中
            </Button>
            <Button
              disabled={submit.isPending}
              onClick={() => {
                if (window.confirm(`确认重放 ? 上限 ${maxCount} 条 , 网页不会重新抓取`))
                  submit.mutate(buildParams())
              }}
            >
              <Play /> 开始重放
            </Button>
          </div>
          {preview.data && (
            <div className="text-xs text-muted-foreground sm:col-span-2 lg:col-span-3">
              预览 : 命中 <span className="font-semibold text-foreground">{preview.data.count}</span> 条 origin
            </div>
          )}
          {preview.isError && (
            <div className="text-xs text-destructive sm:col-span-2 lg:col-span-3">
              预览失败 : {(preview.error as Error).message}
            </div>
          )}
          {submit.isError && (
            <div className="text-xs text-destructive sm:col-span-2 lg:col-span-3">
              提交失败 : {(submit.error as Error).message}
            </div>
          )}
        </CardContent>
      </Card>

      <SectionTitle title="任务记录" extra={<span className="text-xs text-muted-foreground">3 秒自动刷新</span>} />
      {tasks.isPending ? (
        <Skeleton className="h-48" />
      ) : tasks.isError ? (
        <ErrorState error={tasks.error} />
      ) : !tasks.data?.length ? (
        <EmptyState text="还没有重放任务" />
      ) : (
        <Card>
          <CardContent className="pt-5">
            <Table>
              <THead>
                <TR>
                  <TH>ID</TH>
                  <TH>筛选</TH>
                  <TH>状态</TH>
                  <TH>进度 ( 成功 / 失败 )</TH>
                  <TH>说明</TH>
                  <TH>开始时间</TH>
                </TR>
              </THead>
              <TBody>
                {tasks.data.map((task) => {
                  const status = TASK_STATUS[task.status]
                  return (
                    <TR key={task.id}>
                      <TD className="tabular-nums text-muted-foreground">{task.id}</TD>
                      <TD>{filterSummary(task)}</TD>
                      <TD>
                        <Badge variant={(status?.tone as 'default') ?? 'secondary'}>
                          {status?.label ?? task.status}
                        </Badge>
                      </TD>
                      <TD className="tabular-nums">
                        {task.total} ( {task.successCount} / {task.failCount} )
                      </TD>
                      <TD className="max-w-56 truncate text-xs text-muted-foreground" title={task.message ?? ''}>
                        {task.message ?? '—'}
                      </TD>
                      <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                        {fmtDateTime(task.createTime)}
                      </TD>
                    </TR>
                  )
                })}
              </TBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
