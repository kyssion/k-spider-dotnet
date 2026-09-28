/** 展示层的通用格式化与字典映射 */

const SOURCE_NAMES: Record<number, string> = {
  1: '东方财富',
  2: '财联社',
  3: '新浪财经',
  4: '华尔街见闻',
  5: '金十数据',
}

export function sourceName(id: number | null | undefined): string {
  return (id != null && SOURCE_NAMES[id]) || `源 ${id ?? '?'}`
}

/** 网页管线状态机 : 0 未下载 → 3 已下载原始 → 1 已解析详情 ; 2 解析失败 / 4 下载失败 */
export const NEWS_STATUS: Record<number, { label: string; tone: string }> = {
  0: { label: '待下载', tone: 'secondary' },
  1: { label: '已解析', tone: 'default' },
  2: { label: '解析失败', tone: 'destructive' },
  3: { label: '已下载', tone: 'outline' },
  4: { label: '下载失败', tone: 'destructive' },
}

/** 快讯重要度 : 1 普通 / 2 重要 / 3 重大 */
export const FLASH_LEVELS: Record<number, { label: string; tone: string }> = {
  1: { label: '普通', tone: 'secondary' },
  2: { label: '重要', tone: 'warning' },
  3: { label: '重大', tone: 'destructive' },
}

export function fmtDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime()) || d.getFullYear() <= 1) return '—'
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

export function fmtDuration(ms: number | null | undefined): string {
  if (ms == null) return '—'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60_000) return `${(ms / 1000).toFixed(1)}s`
  return `${Math.floor(ms / 60_000)}m${Math.round((ms % 60_000) / 1000)}s`
}

/** 相对时间 ( 用于下次触发倒计时 ) */
export function timeUntil(iso: string | null | undefined): string {
  if (!iso) return '—'
  const diff = new Date(iso).getTime() - Date.now()
  if (Number.isNaN(diff)) return '—'
  if (diff <= 0) return '即将执行'
  const s = Math.round(diff / 1000)
  if (s < 60) return `${s} 秒后`
  const m = Math.round(s / 60)
  if (m < 60) return `${m} 分后`
  return `${Math.round(m / 60)} 时后`
}

/** 滞后秒数的人类描述 */
export function lagText(seconds: number | null | undefined): string {
  if (seconds == null) return '无数据'
  if (seconds < 60) return `${seconds} 秒前`
  if (seconds < 3600) return `${Math.round(seconds / 60)} 分钟前`
  return `${(seconds / 3600).toFixed(1)} 小时前`
}

/** 截断文本 */
export function truncate(text: string | null | undefined, max = 60): string {
  if (!text) return ''
  return text.length > max ? text.slice(0, max) + '…' : text
}
