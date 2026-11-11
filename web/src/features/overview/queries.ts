import { useQuery } from '@tanstack/react-query'
import { getOverview } from '../../api/endpoints/overview'
import { queryKeys } from '../../lib/query-keys'

/** 总览查询；staleTime 10s，避免切页回来立即重复统计。 */
export function useOverview() {
  return useQuery({
    queryKey: queryKeys.overview,
    queryFn: getOverview,
    staleTime: 10_000,
  })
}
