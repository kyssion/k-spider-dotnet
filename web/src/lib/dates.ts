const FALLBACK = '—'

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

export function formatUtc(value: string | null | undefined): string {
  return formatDateTime(value, 'UTC')
}

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
