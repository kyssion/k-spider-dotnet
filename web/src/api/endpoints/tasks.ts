import { apiRequest, buildQuery } from '../client'
import type { PageResponse, TaskView } from '../types'

export interface TaskListFilters {
  page: number
  pageSize: number
  status?: string
  nodeId?: string
}

export function listTasks(filters: TaskListFilters): Promise<PageResponse<TaskView>> {
  const query = buildQuery({
    page: filters.page,
    pageSize: filters.pageSize,
    status: filters.status,
    nodeId: filters.nodeId,
  })
  return apiRequest<PageResponse<TaskView>>(`/api/tasks${query}`)
}

export function cancelTask(id: number, reason?: string): Promise<void> {
  const query = buildQuery({ reason })
  return apiRequest<void>(`/api/tasks/${id}/cancel${query}`, { method: 'POST' })
}
