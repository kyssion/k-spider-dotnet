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

export function useChannels(filters: ChannelListFilters) {
  return useQuery({
    queryKey: queryKeys.channels.list(filters),
    queryFn: () => listChannels(filters),
    placeholderData: (previous) => previous,
  })
}

export function useChannel(id: number) {
  return useQuery({
    queryKey: queryKeys.channels.detail(id),
    queryFn: () => getChannel(id),
    enabled: Number.isFinite(id) && id > 0,
  })
}

export function useChannelCategories() {
  return useQuery({
    queryKey: queryKeys.channels.categories,
    queryFn: listChannelCategories,
    staleTime: 60_000,
  })
}

export function usePauseChannel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => pauseChannel(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.channels.all }),
  })
}

export function useResumeChannel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => resumeChannel(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.channels.all }),
  })
}
