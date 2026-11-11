import { apiRequest, buildQuery } from '../client'
import type { PageResponse, TaskView } from '../types'

/** 任务列表筛选；nodeId 使用 uuid 字符串。 */
export interface TaskListFilters {
  page: number
  pageSize: number
  status?: string
  nodeId?: string
}

/**
 * 分页查询任务。
 * @param filters 页码、页大小与可选状态/节点筛选。
 * @returns 任务分页结果。
 */
export function listTasks(filters: TaskListFilters): Promise<PageResponse<TaskView>> {
  const query = buildQuery({
    page: filters.page,
    pageSize: filters.pageSize,
    status: filters.status,
    nodeId: filters.nodeId,
  })
  return apiRequest<PageResponse<TaskView>>(`/api/tasks${query}`)
}

/**
 * 人工取消任务；终态任务会被后端拒绝。
 * @param id 任务主键。
 * @param reason 取消原因；为空时后端使用默认文案。
 * @returns 取消成功时无响应体。
 */
export function cancelTask(id: number, reason?: string): Promise<void> {
  const query = buildQuery({ reason })
  return apiRequest<void>(`/api/tasks/${id}/cancel${query}`, { method: 'POST' })
}
