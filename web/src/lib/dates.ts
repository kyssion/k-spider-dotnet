const FALLBACK = '—'

/**
 * 按指定时区格式化时间。
 *
 * 后端时间统一是 UTC ISO 字符串；时区只在这里参与展示，不进入任何业务判断。
 * 传入非法值时返回占位符，避免页面因后端脏数据崩溃。
 * @param value UTC ISO 时间字符串。
 * @param timeZone 展示时区，如 Asia/Shanghai。
 * @returns 格式化后的本地展示时间；无效值返回占位符。
 */
export function formatDateTime(value: string | null | undefined, timeZone: string): string {
  if (!value) {
    return FALLBACK
  }
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return FALLBACK
  }
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).format(date)
}

/**
 * 以 UTC 显示原始时间，用于排障与对照日志。
 * @param value UTC ISO 时间字符串。
 * @returns UTC 展示时间；无效值返回占位符。
 */
export function formatUtc(value: string | null | undefined): string {
  return formatDateTime(value, 'UTC')
}

/**
 * 生成相对时间文案。
 *
 * `now` 可注入，便于测试；业务上只做展示，不参与排序或调度。
 * @param value UTC ISO 时间字符串。
 * @param now 当前时间戳，默认 Date.now()。
 * @returns 相对时间文案；无效值返回占位符。
 */
export function formatRelative(value: string | null | undefined, now = Date.now()): string {
  if (!value) {
    return FALLBACK
  }
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return FALLBACK
  }
  const diffSeconds = Math.round((date.getTime() - now) / 1000)
  const formatter = new Intl.RelativeTimeFormat('zh-CN', { numeric: 'auto' })
  const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
    ['year', 31_536_000],
    ['month', 2_592_000],
    ['day', 86_400],
    ['hour', 3_600],
    ['minute', 60],
  ]
  for (const [unit, seconds] of units) {
    if (Math.abs(diffSeconds) >= seconds) {
      return formatter.format(Math.round(diffSeconds / seconds), unit)
    }
  }
  return formatter.format(diffSeconds, 'second')
}
