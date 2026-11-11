import type { ChannelListFilters } from '../api/endpoints/channels'
import type { TaskListFilters } from '../api/endpoints/tasks'

/**
 * TanStack Query 的 key 工厂。
 *
 * 所有 key 集中在这里，避免页面与 mutation 各自拼字符串导致失效范围不一致。
 * 列表 key 包含完整筛选对象，筛选变化自然触发新查询。
 */
export const queryKeys = {
  auth: {
    /** 当前登录用户；登录/登出后必须失效。 */
    me: ['auth', 'me'] as const,
  },
  /** 总览统计；写操作不直接失效，刷新按钮可手动重取。 */
  overview: ['overview'] as const,
  channels: {
    /** 渠道相关全部查询的前缀，用于批量失效。 */
    all: ['channels'] as const,
    list: (filters: ChannelListFilters) => ['channels', 'list', filters] as const,
    detail: (id: number) => ['channels', 'detail', id] as const,
    categories: ['channels', 'categories'] as const,
  },
  tasks: {
    /** 任务相关全部查询的前缀，取消任务后统一失效。 */
    all: ['tasks'] as const,
    list: (filters: TaskListFilters) => ['tasks', 'list', filters] as const,
  },
  nodes: ['nodes'] as const,
  users: ['users'] as const,
}
