import { apiRequest, buildQuery } from '../client'
import type { ChannelCategoryView, ChannelView, PageResponse } from '../types'

/** 渠道列表筛选；页码从 1 开始，pageSize 由后端钳制。 */
export interface ChannelListFilters {
  page: number
  pageSize: number
  categoryId?: number
  status?: string
}

/**
 * 分页查询渠道。
 * @param filters 页码、页大小与可选分类/状态。
 * @returns 渠道分页结果。
 */
export function listChannels(filters: ChannelListFilters): Promise<PageResponse<ChannelView>> {
  const query = buildQuery({
    page: filters.page,
    pageSize: filters.pageSize,
    categoryId: filters.categoryId,
    status: filters.status,
  })
  return apiRequest<PageResponse<ChannelView>>(`/api/channels${query}`)
}

/**
 * 读取单个渠道详情。
 * @param id 渠道主键。
 * @returns 渠道详情。
 */
export function getChannel(id: number): Promise<ChannelView> {
  return apiRequest<ChannelView>(`/api/channels/${id}`)
}

/**
 * 读取渠道分类树；分类变化频率低，页面可缓存更久。
 * @returns 分类树节点列表。
 */
export function listChannelCategories(): Promise<ChannelCategoryView[]> {
  return apiRequest<ChannelCategoryView[]>('/api/channel-categories')
}

/**
 * 暂停渠道；已暂停时后端幂等返回成功。
 * @param id 渠道主键。
 * @returns 暂停成功时无响应体。
 */
export function pauseChannel(id: number): Promise<void> {
  return apiRequest<void>(`/api/channels/${id}/pause`, { method: 'POST' })
}

/**
 * 恢复渠道；retired 渠道会被后端拒绝。
 * @param id 渠道主键。
 * @returns 恢复成功时无响应体。
 */
export function resumeChannel(id: number): Promise<void> {
  return apiRequest<void>(`/api/channels/${id}/resume`, { method: 'POST' })
}
