import { useQuery } from '@tanstack/react-query'
import { getOverview } from '../../api/endpoints/overview'
import { queryKeys } from '../../lib/query-keys'

export function useOverview() {
  return useQuery({
    queryKey: queryKeys.overview,
    queryFn: getOverview,
    staleTime: 10_000,
  })
}
