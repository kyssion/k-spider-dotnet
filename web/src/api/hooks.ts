import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, type QueryParams } from './client'
import type {
  DistributeDto,
  FlashItem,
  FlashLag,
  JobCommand,
  JobState,
  NewsDetail,
  NewsListItem,
  NodeStatus,
  PageResult,
  PipelineStatus,
  VolumeDto,
} from './types'

/** —— 运行状态 —— */

export function useJobs() {
  return useQuery({
    queryKey: ['jobs'],
    queryFn: () => apiGet<JobState[]>('/status/jobs'),
    refetchInterval: 5_000,
  })
}

export function usePipeline() {
  return useQuery({
    queryKey: ['pipeline'],
    queryFn: () => apiGet<PipelineStatus[]>('/status/pipeline'),
    refetchInterval: 30_000,
  })
}

export function useFlashLag() {
  return useQuery({
    queryKey: ['flash-lag'],
    queryFn: () => apiGet<FlashLag[]>('/status/flash-lag'),
    refetchInterval: 15_000,
  })
}

export function useNodes() {
  return useQuery({
    queryKey: ['nodes'],
    queryFn: () => apiGet<NodeStatus[]>('/status/nodes'),
    refetchInterval: 30_000,
  })
}

/** —— 数据查询 —— */

export function useNewsList(params: QueryParams & { page: number; pageSize: number }) {
  return useQuery({
    queryKey: ['news-list', params],
    queryFn: () => apiGet<PageResult<NewsListItem>>('/news/list', params),
    placeholderData: keepPreviousData,
  })
}

export function useNewsDetail(newsUrl: string | null) {
  return useQuery({
    queryKey: ['news-detail', newsUrl],
    queryFn: () => apiGet<NewsDetail>('/news/detail', { newsUrl: newsUrl! }),
    enabled: newsUrl != null,
  })
}

export function useFlashList(params: QueryParams & { page: number; pageSize: number }) {
  return useQuery({
    queryKey: ['flash-list', params],
    queryFn: () => apiGet<PageResult<FlashItem>>('/flash/list', params),
    placeholderData: keepPreviousData,
  })
}

/** —— 数据分析 —— */

export function useVolume(params: { start: string; end: string; granularity: string }) {
  return useQuery({
    queryKey: ['volume', params],
    queryFn: () => apiGet<VolumeDto>('/analysis/volume', params),
  })
}

export function useDistribute(params: { start: string; end: string }) {
  return useQuery({
    queryKey: ['distribute', params],
    queryFn: () => apiGet<DistributeDto>('/analysis/distribute', params),
  })
}

export function useKeywords(params: { start: string; end: string; top: number }) {
  return useQuery({
    queryKey: ['keywords', params],
    queryFn: () => apiGet<{ key: string; count: number }[]>('/analysis/keywords', params),
  })
}

/** —— 任务控制 —— */

export function useCommands() {
  return useQuery({
    queryKey: ['commands'],
    queryFn: () => apiGet<JobCommand[]>('/jobs/commands', { limit: 50 }),
    refetchInterval: 3_000,
  })
}

export function useSendCommand() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (vars: { jobName: string; action: string }) =>
      apiPost<JobCommand>(`/jobs/${vars.jobName}/${vars.action}`),
    onSuccess: () => {
      // 指令约 3 秒内被消费 , 加快指令历史与任务状态的刷新
      queryClient.invalidateQueries({ queryKey: ['commands'] })
      queryClient.invalidateQueries({ queryKey: ['jobs'] })
    },
  })
}
