import * as React from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { useDistribute, useKeywords, useVolume } from '../api/hooks'
import { Badge } from '../components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Input, Select } from '../components/ui/input'
import { EmptyState, ErrorState, PageTitle, Skeleton } from '../components/ui/states'
import { FLASH_LEVELS, sourceName } from '../lib/format'
import { useAnalysisFilter } from '../stores/filters'

const PIE_COLORS = ['#2563eb', '#16a34a', '#d97706', '#dc2626', '#7c3aed', '#0891b2']

export default function AnalysisPage() {
  const { start, end, granularity, setRange, setGranularity } = useAnalysisFilter()
  const range = { start, end }
  // 分布数据一次拉取 , 四张卡共用
  const { data: distribute } = useDistribute(range)
  const distributeData = distribute ?? {
    newsSource: [],
    newsCategory: [],
    flashSource: [],
    flashLevel: [],
  }

  return (
    <div className="mx-auto max-w-6xl">
      <PageTitle title="数据分析" description="入库量趋势 · 分布 · 关键词 ( 时间窗最大 31 天 )" />
      <Card className="mb-6">
        <CardContent className="flex flex-wrap items-center gap-2 pt-5 text-sm">
          <span className="text-muted-foreground">时间窗</span>
          <Input
            type="date"
            value={start}
            onChange={(e) => setRange(e.target.value || start, end)}
          />
          <span className="text-muted-foreground">至</span>
          <Input type="date" value={end} onChange={(e) => setRange(start, e.target.value || end)} />
          <span className="ml-4 text-muted-foreground">粒度</span>
          <Select
            value={granularity}
            onChange={(e) => setGranularity(e.target.value === 'hour' ? 'hour' : 'day')}
          >
            <option value="hour">按小时</option>
            <option value="day">按天</option>
          </Select>
        </CardContent>
      </Card>

      <VolumeCard range={range} granularity={granularity} />
      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <SourceCard title="网页新闻 · 按来源" rows={distributeData.newsSource} />
        <PieCard title="快讯 · 按来源" rows={distributeData.flashSource} />
        <SourceCard title="网页新闻 · 按栏目号" rows={distributeData.newsCategory} />
        <LevelCard rows={distributeData.flashLevel} />
      </div>
      <KeywordsCard range={range} />
    </div>
  )
}

function VolumeCard({
  range,
  granularity,
}: {
  range: { start: string; end: string }
  granularity: string
}) {
  const { data, isPending, error } = useVolume({ ...range, granularity })
  // 两张表各自按 bucket 汇总成一条序列 ( 不区分源 , 看总量趋势 )
  const series = React.useMemo(() => {
    if (!data) return []
    const buckets = new Map<string, { news: number; flash: number }>()
    for (const row of data.news) {
      const entry = buckets.get(row.bucket) ?? { news: 0, flash: 0 }
      entry.news += row.count
      buckets.set(row.bucket, entry)
    }
    for (const row of data.flash) {
      const entry = buckets.get(row.bucket) ?? { news: 0, flash: 0 }
      entry.flash += row.count
      buckets.set(row.bucket, entry)
    }
    return [...buckets.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([bucket, counts]) => ({
        bucket: bucket.slice(0, 16).replace('T', ' '),
        ...counts,
      }))
  }, [data])

  return (
    <Card>
      <CardHeader>
        <CardTitle>入库量趋势</CardTitle>
      </CardHeader>
      <CardContent>
        {isPending ? (
          <Skeleton className="h-72" />
        ) : error ? (
          <ErrorState error={error} />
        ) : series.length === 0 ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <LineChart data={series} margin={{ top: 8, right: 16, bottom: 0, left: 0 }}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="bucket" tick={{ fontSize: 11 }} interval="preserveStartEnd" />
              <YAxis tick={{ fontSize: 11 }} allowDecimals={false} width={40} />
              <Tooltip
                contentStyle={{ fontSize: 12, borderRadius: 8 }}
                labelStyle={{ fontSize: 12 }}
              />
              <Legend wrapperStyle={{ fontSize: 12 }} />
              <Line type="monotone" dataKey="news" name="网页新闻" stroke="#2563eb" dot={false} strokeWidth={2} />
              <Line type="monotone" dataKey="flash" name="实时快讯" stroke="#16a34a" dot={false} strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}

function SourceCard({
  title,
  rows,
}: {
  title: string
  rows: { key: string; count: number }[]
}) {
  const data = rows.map((row) => ({
    name: /^\d+$/.test(row.key) && Number(row.key) <= 5 ? sourceName(Number(row.key)) : row.key,
    count: row.count,
  }))
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} allowDecimals={false} width={40} />
              <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Bar dataKey="count" name="条数" fill="#2563eb" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}

function PieCard({ title, rows }: { title: string; rows: { key: string; count: number }[] }) {
  const data = rows.map((row) => ({
    name: sourceName(Number(row.key)),
    value: row.count,
  }))
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie data={data} dataKey="value" nameKey="name" innerRadius={50} outerRadius={80} paddingAngle={2}>
                {data.map((_, index) => (
                  <Cell key={index} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Legend wrapperStyle={{ fontSize: 12 }} />
            </PieChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}

function LevelCard({ rows }: { rows: { key: string; count: number }[] }) {
  const data = rows.map((row) => ({
    name: FLASH_LEVELS[Number(row.key)]?.label ?? row.key,
    count: row.count,
  }))
  const color = (name: string) =>
    name === '重大' ? '#dc2626' : name === '重要' ? '#d97706' : '#64748b'
  return (
    <Card>
      <CardHeader>
        <CardTitle>快讯 · 按重要度</CardTitle>
      </CardHeader>
      <CardContent>
        {data.length === 0 ? (
          <EmptyState />
        ) : (
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} allowDecimals={false} width={40} />
              <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Bar dataKey="count" name="条数" radius={[4, 4, 0, 0]}>
                {data.map((row, index) => (
                  <Cell key={index} fill={color(row.name)} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}

function KeywordsCard({ range }: { range: { start: string; end: string } }) {
  const { data, isPending, error } = useKeywords({ ...range, top: 50 })
  return (
    <Card className="mt-6">
      <CardHeader>
        <CardTitle>关键词 Top 50</CardTitle>
      </CardHeader>
      <CardContent>
        {isPending ? (
          <Skeleton className="h-24" />
        ) : error ? (
          <ErrorState error={error} />
        ) : !data?.length ? (
          <EmptyState text="暂无标签数据" />
        ) : (
          <div className="flex flex-wrap gap-2">
            {data.map((row) => (
              <Badge key={row.key} variant={row.count >= data[0].count / 4 ? 'default' : 'secondary'}>
                {row.key}
                <span className="ml-1 opacity-60 tabular-nums">{row.count}</span>
              </Badge>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
