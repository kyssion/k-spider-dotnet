import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getChannel,
  listChannelCategories,
  listChannels,
  pauseChannel,
  resumeChannel,
  type ChannelListFilters,
} from '../../api/endpoints/channels'
import { queryKeys } from '../../lib/query-keys'

/** 渠道列表；placeholderData 保留上一页，翻页时不闪空表。 */
export function useChannels(filters: ChannelListFilters) {
  return useQuery({
    queryKey: queryKeys.channels.list(filters),
    queryFn: () => listChannels(filters),
    placeholderData: (previous) => previous,
  })
}

/** 渠道详情；id 非法时不发请求。 */
export function useChannel(id: number) {
  return useQuery({
    queryKey: queryKeys.channels.detail(id),
    queryFn: () => getChannel(id),
    enabled: Number.isFinite(id) && id > 0,
  })
}

/** 分类树；变化频率低，缓存 60s。 */
export function useChannelCategories() {
  return useQuery({
    queryKey: queryKeys.channels.categories,
    queryFn: listChannelCategories,
    staleTime: 60_000,
  })
}

/** 暂停渠道；成功后失效渠道全部查询（列表+详情）。 */
export function usePauseChannel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => pauseChannel(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.channels.all }),
  })
}

/** 恢复渠道；成功后失效渠道全部查询。 */
export function useResumeChannel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => resumeChannel(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.channels.all }),
  })
}
