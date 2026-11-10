import { apiRequest, buildQuery } from '../client'
import type { ChannelCategoryView, ChannelView, PageResponse } from '../types'

export interface ChannelListFilters {
  page: number
  pageSize: number
  categoryId?: number
  status?: string
}

export function listChannels(filters: ChannelListFilters): Promise<PageResponse<ChannelView>> {
  const query = buildQuery({
    page: filters.page,
    pageSize: filters.pageSize,
    categoryId: filters.categoryId,
    status: filters.status,
  })
  return apiRequest<PageResponse<ChannelView>>(`/api/channels${query}`)
}

export function getChannel(id: number): Promise<ChannelView> {
  return apiRequest<ChannelView>(`/api/channels/${id}`)
}

export function listChannelCategories(): Promise<ChannelCategoryView[]> {
  return apiRequest<ChannelCategoryView[]>('/api/channel-categories')
}

export function pauseChannel(id: number): Promise<void> {
  return apiRequest<void>(`/api/channels/${id}/pause`, { method: 'POST' })
}

export function resumeChannel(id: number): Promise<void> {
  return apiRequest<void>(`/api/channels/${id}/resume`, { method: 'POST' })
}
