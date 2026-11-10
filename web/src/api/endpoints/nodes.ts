import { apiRequest } from '../client'
import type { NodeView } from '../types'

export function listNodes(): Promise<NodeView[]> {
  return apiRequest<NodeView[]>('/api/nodes')
}
