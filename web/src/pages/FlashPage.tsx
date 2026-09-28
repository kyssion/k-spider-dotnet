import * as React from 'react'
import { useFlashList } from '../api/hooks'
import { Badge } from '../components/ui/badge'
import { Card, CardContent } from '../components/ui/card'
import { Dialog } from '../components/ui/dialog'
import { Input, Select } from '../components/ui/input'
import { EmptyState, ErrorState, PageTitle, Skeleton } from '../components/ui/states'
import { Table, TBody, TD, TH, THead, TR } from '../components/ui/table'
import { Pager } from '../components/Pager'
import { FLASH_LEVELS, fmtDateTime, sourceName, truncate } from '../lib/format'

export default function FlashPage() {
  const [page, setPage] = React.useState(1)
  const [source, setSource] = React.useState('')
  const [level, setLevel] = React.useState('')
  const [keyword, setKeyword] = React.useState('')
  const [start, setStart] = React.useState('')
  const [end, setEnd] = React.useState('')
  const [active, setActive] = React.useState<number | null>(null)

  const { data, isPending, error, isFetching } = useFlashList({
    page,
    pageSize: 30,
    source,
    level,
    keyword,
    start,
    end,
  })
  const activeItem = data?.items.find((item) => item.id === active) ?? null

  return (
    <div className="mx-auto max-w-6xl">
      <PageTitle title="实时快讯" description="spider_flash_news · 拉到即终态的四源快讯" />
      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-2 pt-5">
          <Select aria-label="来源" value={source} onChange={(e) => { setSource(e.target.value); setPage(1) }}>
            <option value="">全部来源</option>
            {[2, 3, 4, 5].map((id) => (
              <option key={id} value={id}>{sourceName(id)}</option>
            ))}
          </Select>
          <Select aria-label="级别" value={level} onChange={(e) => { setLevel(e.target.value); setPage(1) }}>
            <option value="">全部级别</option>
            {Object.entries(FLASH_LEVELS).map(([code, info]) => (
              <option key={code} value={code}>{info.label}</option>
            ))}
          </Select>
          <Input type="date" value={start} onChange={(e) => { setStart(e.target.value); setPage(1) }} />
          <Input type="date" value={end} onChange={(e) => { setEnd(e.target.value); setPage(1) }} />
          <Input
            placeholder="标题关键词"
            value={keyword}
            onChange={(e) => { setKeyword(e.target.value); setPage(1) }}
            className="w-44"
          />
          {isFetching && <span className="text-xs text-muted-foreground">加载中…</span>}
        </CardContent>
      </Card>

      {isPending ? (
        <Skeleton className="h-72" />
      ) : error ? (
        <ErrorState error={error} />
      ) : !data?.items.length ? (
        <EmptyState text="没有符合条件的快讯" />
      ) : (
        <Card>
          <CardContent className="pt-5">
            <Table>
              <THead>
                <TR>
                  <TH>时间</TH>
                  <TH>来源</TH>
                  <TH>级别</TH>
                  <TH>标题 / 内容</TH>
                  <TH>标签</TH>
                </TR>
              </THead>
              <TBody>
                {data.items.map((item) => {
                  const level = FLASH_LEVELS[item.level]
                  return (
                    <TR key={item.id} className="cursor-pointer" onClick={() => setActive(item.id)}>
                      <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                        {fmtDateTime(item.newsTime)}
                      </TD>
                      <TD className="whitespace-nowrap">{sourceName(item.fromMedia)}</TD>
                      <TD>
                        <Badge variant={(level?.tone as 'default') ?? 'secondary'}>
                          {level?.label ?? item.level}
                        </Badge>
                      </TD>
                      <TD className="max-w-lg">
                        <div className={item.level >= 3 ? 'font-semibold text-destructive' : ''}>
                          {truncate(item.title ?? item.content, 60) || '(空)'}
                        </div>
                      </TD>
                      <TD className="max-w-40 truncate text-xs text-muted-foreground">
                        {truncate(item.keyword, 30) || '—'}
                      </TD>
                    </TR>
                  )
                })}
              </TBody>
            </Table>
          </CardContent>
        </Card>
      )}
      {data && <Pager page={page} pageSize={30} total={data.total} onChange={setPage} />}

      <Dialog
        open={activeItem != null}
        onClose={() => setActive(null)}
        title={activeItem ? `${sourceName(activeItem.fromMedia)} · ${fmtDateTime(activeItem.newsTime)}` : ''}
      >
        {activeItem && (
          <div className="space-y-4 text-sm">
            <div className="flex items-center gap-2">
              <Badge variant={(FLASH_LEVELS[activeItem.level]?.tone as 'default') ?? 'secondary'}>
                {FLASH_LEVELS[activeItem.level]?.label ?? activeItem.level}
              </Badge>
              {activeItem.newsUrl && (
                <a
                  className="text-xs text-primary hover:underline"
                  href={activeItem.newsUrl}
                  target="_blank"
                  rel="noreferrer"
                >
                  原文链接
                </a>
              )}
            </div>
            {activeItem.title && <h3 className="font-semibold leading-relaxed">{activeItem.title}</h3>}
            <div className="whitespace-pre-wrap rounded-lg border p-3 text-sm leading-relaxed">
              {activeItem.content || '(正文为空 , 可能是 PLUS 专享条目)'}
            </div>
            {activeItem.keyword && (
              <div className="flex flex-wrap gap-1">
                {activeItem.keyword.split(',').filter(Boolean).map((word) => (
                  <Badge key={word} variant="secondary">{word.trim()}</Badge>
                ))}
              </div>
            )}
            {activeItem.stockList && (
              <div className="text-xs text-muted-foreground">
                关联标的 : {activeItem.stockList}
              </div>
            )}
          </div>
        )}
      </Dialog>
    </div>
  )
}
