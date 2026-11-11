import { useQuery } from '@tanstack/react-query'
import { listNodes } from '../../api/endpoints/nodes'
import { queryKeys } from '../../lib/query-keys'

/** 节点镜像；10s 内复用，避免频繁刷新。 */
export function useNodes() {
  return useQuery({
    queryKey: queryKeys.nodes,
    queryFn: listNodes,
    staleTime: 10_000,
  })
}
