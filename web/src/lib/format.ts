/**
 * 计数使用中文千分位，保证控制台表格可读性。
 * @param value 非负计数。
 * @returns 千分位字符串。
 */
export function formatNumber(value: number): string {
  return new Intl.NumberFormat('zh-CN').format(value)
}

/**
 * 原始载荷大小按 B/KB/MB 展示；用于判断列表是否过大。
 * @param value 字节数。
 * @returns 带单位的可读大小。
 */
export function formatBytes(value: number): string {
  if (value < 1024) {
    return `${value} B`
  }
  if (value < 1024 * 1024) {
    return `${(value / 1024).toFixed(1)} KB`
  }
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

/**
 * 截断长文本。
 *
 * 只用于展示（任务包 JSON、错误信息）；不改变服务端原始值。
 * @param value 原始文本。
 * @param max 最大保留字符数。
 * @returns 截断后的文本；未超长时原样返回。
 */
export function truncate(value: string, max = 80): string {
  return value.length <= max ? value : `${value.slice(0, max)}…`
}
