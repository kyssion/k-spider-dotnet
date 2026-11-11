import { apiRequest } from '../client'
import type { OverviewResponse } from '../types'

/**
 * 读取总览统计；页面负责加载/错误/空态。
 * @returns 渠道、任务、节点统计与最近失败。
 */
export function getOverview(): Promise<OverviewResponse> {
  return apiRequest<OverviewResponse>('/api/overview')
}
