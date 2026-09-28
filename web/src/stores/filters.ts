import { create } from 'zustand'

/** 分析页共享的时间窗与粒度 ( 趋势 / 分布 / 关键词 同步联动 ) */
interface AnalysisFilterState {
  start: string
  end: string
  granularity: 'hour' | 'day'
  setRange: (start: string, end: string) => void
  setGranularity: (granularity: 'hour' | 'day') => void
}

function defaultRange(): { start: string; end: string } {
  const now = new Date()
  const end = toDateStr(now)
  const start = toDateStr(new Date(now.getTime() - 7 * 86_400_000))
  return { start, end }
}

function toDateStr(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

export const useAnalysisFilter = create<AnalysisFilterState>((set) => ({
  ...defaultRange(),
  granularity: 'day',
  setRange: (start, end) => set({ start, end }),
  setGranularity: (granularity) => set({ granularity }),
}))
