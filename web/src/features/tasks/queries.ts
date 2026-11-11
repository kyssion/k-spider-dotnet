import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cancelTask, listTasks, type TaskListFilters } from '../../api/endpoints/tasks'
import { queryKeys } from '../../lib/query-keys'

/** 任务列表；保留上一页数据避免分页闪烁。 */
export function useTasks(filters: TaskListFilters) {
  return useQuery({
    queryKey: queryKeys.tasks.list(filters),
    queryFn: () => listTasks(filters),
    placeholderData: (previous) => previous,
  })
}

/** 取消任务；成功后失效任务列表。 */
export function useCancelTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { id: number; reason?: string }) => cancelTask(input.id, input.reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all }),
  })
}
