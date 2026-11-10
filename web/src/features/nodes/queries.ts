import { useQuery } from '@tanstack/react-query'
import { listNodes } from '../../api/endpoints/nodes'
import { queryKeys } from '../../lib/query-keys'

export function useNodes() {
  return useQuery({
    queryKey: queryKeys.nodes,
    queryFn: listNodes,
    staleTime: 10_000,
  })
}
