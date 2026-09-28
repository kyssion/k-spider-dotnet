import * as React from 'react'
import { ExternalLink } from 'lucide-react'
import { useNewsDetail, useNewsList } from '../api/hooks'
import { Badge } from '../components/ui/badge'
import { Card, CardContent } from '../components/ui/card'
import { Dialog } from '../components/ui/dialog'
import { Input, Select } from '../components/ui/input'
import { EmptyState, ErrorState, PageTitle, Skeleton } from '../components/ui/states'
import { Table, TBody, TD, TH, THead, TR } from '../components/ui/table'
import { Pager } from '../components/Pager'
import { fmtDateTime, NEWS_STATUS, sourceName, truncate } from '../lib/format'

export default function NewsPage() {
  const [page, setPage] = React.useState(1)
  const [source, setSource] = React.useState('')
  const [status, setStatus] = React.useState('')
  const [keyword, setKeyword] = React.useState('')
  const [start, setStart] = React.useState('')
  const [end, setEnd] = React.useState('')
  const [detailUrl, setDetailUrl] = React.useState<string | null>(null)

  const { data, isPending, error, isFetching } = useNewsList({
    page,
    pageSize: 20,
    source,
    status,
    keyword,
    start,
    end,
  })

  return (
    <div className="mx-auto max-w-6xl">
      <PageTitle title="网页新闻" description="spider_news_list 三段流水线的列表数据" />
      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-2 pt-5">
          <Select aria-label="来源" value={source} onChange={(e) => { setSource(e.target.value); setPage(1) }}>
            <option value="">全部来源</option>
            {[1, 2].map((id) => (
              <option key={id} value={id}>{sourceName(id)}</option>
            ))}
          </Select>
          <Select aria-label="状态" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1) }}>
            <option value="">全部状态</option>
            {Object.entries(NEWS_STATUS).map(([code, info]) => (
              <option key={code} value={code}>{info.label}</option>
            ))}
          </Select>
          <Input
            type="date"
            value={start}
            onChange={(e) => { setStart(e.target.value); setPage(1) }}
            title="开始时间"
          />
          <Input
            type="date"
            value={end}
            onChange={(e) => { setEnd(e.target.value); setPage(1) }}
            title="结束时间"
          />
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
        <EmptyState text="没有符合条件的新闻" />
      ) : (
        <Card>
          <CardContent className="pt-5">
            <Table>
              <THead>
                <TR>
                  <TH>发布时间</TH>
                  <TH>标题</TH>
                  <TH>来源</TH>
                  <TH>状态</TH>
                  <TH className="text-right">失败</TH>
                </TR>
              </THead>
              <TBody>
                {data.items.map((item) => {
                  const info = NEWS_STATUS[item.downloadStatusCode]
                  return (
                    <TR key={item.id} className="cursor-pointer" onClick={() => setDetailUrl(item.newsUrl)}>
                      <TD className="tabular-nums whitespace-nowrap text-muted-foreground">
                        {fmtDateTime(item.newsTime)}
                      </TD>
                      <TD className="max-w-md">{truncate(item.newsTitle, 50) || '(无标题)'}</TD>
                      <TD className="whitespace-nowrap">{sourceName(item.fromMedia)}</TD>
                      <TD>
                        <Badge variant={(info?.tone as 'default') ?? 'outline'}>
                          {info?.label ?? item.downloadStatusCode}
                        </Badge>
                      </TD>
                      <TD className="text-right tabular-nums">
                        {item.failCount > 0 ? item.failCount : '—'}
                      </TD>
                    </TR>
                  )
                })}
              </TBody>
            </Table>
          </CardContent>
        </Card>
      )}
      {data && (
        <Pager page={page} pageSize={20} total={data.total} onChange={setPage} />
      )}

      <NewsDetailDialog newsUrl={detailUrl} onClose={() => setDetailUrl(null)} />
    </div>
  )
}

function NewsDetailDialog({ newsUrl, onClose }: { newsUrl: string | null; onClose: () => void }) {
  const { data, isPending, error } = useNewsDetail(newsUrl)
  return (
    <Dialog open={newsUrl != null} onClose={onClose} title="新闻详情">
      {isPending ? (
        <Skeleton className="h-64" />
      ) : error ? (
        <ErrorState error={error} />
      ) : data ? (
        <div className="space-y-5 text-sm">
          <section>
            <h3 className="mb-2 font-semibold">{data.list.newsTitle || '(无标题)'}</h3>
            <div className="grid grid-cols-2 gap-x-6 gap-y-1 text-xs text-muted-foreground sm:grid-cols-3">
              <div>来源 : {sourceName(data.list.fromMedia)}</div>
              <div>发布 : {fmtDateTime(data.list.newsTime)}</div>
              <div>栏目 : {data.list.category}</div>
              <div>状态 : {NEWS_STATUS[data.list.downloadStatusCode]?.label}</div>
              <div className="col-span-2 truncate">
                URL :{' '}
                {data.list.newsUrl && (
                  <a
                    className="text-primary hover:underline"
                    href={data.list.newsUrl}
                    target="_blank"
                    rel="noreferrer"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {truncate(data.list.newsUrl, 80)} <ExternalLink className="inline size-3" />
                  </a>
                )}
              </div>
            </div>
            {data.list.newsSummary && (
              <p className="mt-3 rounded-lg bg-muted p-3 text-xs leading-relaxed">
                {data.list.newsSummary}
              </p>
            )}
          </section>

          {data.content && (
            <section>
              <div className="mb-2 text-xs font-medium text-muted-foreground">解析正文</div>
              <div className="max-h-72 overflow-auto whitespace-pre-wrap rounded-lg border p-3 text-xs leading-relaxed">
                {data.content.newsContentText || '(正文为空)'}
              </div>
              {data.content.newsKeyword && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {data.content.newsKeyword.split(',').filter(Boolean).map((word) => (
                    <Badge key={word} variant="secondary">{word.trim()}</Badge>
                  ))}
                </div>
              )}
            </section>
          )}

          {data.images.length > 0 && (
            <section>
              <div className="mb-2 text-xs font-medium text-muted-foreground">
                图片 ({data.images.length})
              </div>
              <div className="flex flex-wrap gap-2">
                {data.images.map((image) => (
                  <a key={image.imageResourceUrl} href={image.imageResourceUrl ?? '#'} target="_blank" rel="noreferrer">
                    <img
                      src={image.imageResourceUrl ?? ''}
                      alt={image.imageName ?? ''}
                      className="h-20 w-32 rounded-lg border object-cover"
                      loading="lazy"
                      referrerPolicy="no-referrer"
                    />
                  </a>
                ))}
              </div>
            </section>
          )}

          <details>
            <summary className="cursor-pointer text-xs text-muted-foreground">
              原始内容 ( JSON )
              {data.origin?.status === 2 && (
                <Badge variant="destructive" className="ml-2">下载失败 : {data.origin.message}</Badge>
              )}
            </summary>
            <pre className="mt-2 max-h-72 overflow-auto rounded-lg bg-muted p-3 text-[11px] leading-relaxed">
              {data.origin?.newsOriginContent ?? '(未下载)'}
            </pre>
          </details>
        </div>
      ) : (
        <EmptyState text="新闻不存在" />
      )}
    </Dialog>
  )
}
