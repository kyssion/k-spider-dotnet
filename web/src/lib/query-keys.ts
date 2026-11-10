import type { ChannelListFilters } from '../api/endpoints/channels'
import type { TaskListFilters } from '../api/endpoints/tasks'

export const queryKeys = {
  auth: {
    me: ['auth', 'me'] as const,
  },
  overview: ['overview'] as const,
  channels: {
    all: ['channels'] as const,
    list: (filters: ChannelListFilters) => ['channels', 'list', filters] as const,
    detail: (id: number) => ['channels', 'detail', id] as const,
    categories: ['channels', 'categories'] as const,
  },
  tasks: {
    all: ['tasks'] as const,
    list: (filters: TaskListFilters) => ['tasks', 'list', filters] as const,
  },
  nodes: ['nodes'] as const,
  users: ['users'] as const,
}
