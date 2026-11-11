import { apiRequest } from '../client'
import type { NodeView } from '../types'

/**
 * 读取节点镜像列表；实时在线状态以 admin 内存为准。
 * @returns 节点镜像列表。
 */
export function listNodes(): Promise<NodeView[]> {
  return apiRequest<NodeView[]>('/api/nodes')
}
