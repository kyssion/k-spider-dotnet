export interface PageResponse<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface Me {
  id: number
  username: string
  displayName: string | null
  csrfToken: string
}

export interface UserView {
  id: number
  username: string
  displayName: string | null
  status: string
  createTime: string
}

export interface ChannelView {
  id: number
  categoryId: number
  channelName: string
  sourceAdapter: string
  detailAdapter: string
  writeMode: string
  retentionDays: number | null
  status: string
  scheduleJson: string
  extraJson: string
  createTime: string
  updateTime: string
}

export interface ChannelCategoryView {
  id: number
  parentId: number | null
  name: string
  path: string
  sortNo: number
  status: string
}

export interface TaskView {
  id: number
  nodeId: string
  taskType: string
  taskDetailJson: string
  status: string
  extraJson: string
  createTime: string
  updateTime: string
}

export interface NodeView {
  nodeId: string
  nodeType: string
  status: string
  deviceJson: string | null
  extraJson: string
  createTime: string
  updateTime: string
}

export interface OverviewResponse {
  generatedAt: string
  channels: {
    total: number
    active: number
    paused: number
    retired: number
  }
  tasks: {
    total: number
    byStatus: Record<string, number>
  }
  nodes: {
    total: number
    online: number
    offline: number
  }
  recentFailures: RecentFailureView[]
}

export interface RecentFailureView {
  taskId: number
  channelId: number
  nodeId: string
  status: string
  failCount: number
  lastError: string | null
  updatedAt: string
}

export interface LoginInput {
  username: string
  password: string
  rememberMe: boolean
}

export interface UserCreateInput {
  username: string
  password: string
  displayName: string | null
}

export interface UserPatchInput {
  displayName?: string | null
  status?: string
}
