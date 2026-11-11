/**
 * 与 admin REST 契约对应的 DTO。
 *
 * 时间字段都是 UTC ISO 字符串；展示时区只在 lib/dates 中处理。
 * admin 契约变更时，必须同步本文件与对应规格（0002/0004 等）。
 */

/** admin 分页响应的统一形状。 */
export interface PageResponse<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

/** 当前登录用户；csrfToken 只保存在内存，不写 localStorage。 */
export interface Me {
  id: number
  username: string
  displayName: string | null
  csrfToken: string
}

/** 账号列表/详情视图。 */
export interface UserView {
  id: number
  username: string
  displayName: string | null
  status: string
  createTime: string
}

/** 渠道视图；schedule/extra 保留原始 JSON 文本供排障。 */
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

/** 渠道分类树节点。 */
export interface ChannelCategoryView {
  id: number
  parentId: number | null
  name: string
  path: string
  sortNo: number
  status: string
}

/** 任务视图；taskDetailJson/extraJson 由页面按需解析。 */
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

/** 节点镜像视图；实时在线状态以 admin 内存为准。 */
export interface NodeView {
  nodeId: string
  nodeType: string
  status: string
  deviceJson: string | null
  extraJson: string
  createTime: string
  updateTime: string
}

/** 总览响应；generatedAt 为 UTC。 */
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

/** 最近失败任务摘要。 */
export interface RecentFailureView {
  taskId: number
  channelId: number
  nodeId: string
  status: string
  failCount: number
  lastError: string | null
  updatedAt: string
}

/** 登录请求；rememberMe 控制持久 Cookie。 */
export interface LoginInput {
  username: string
  password: string
  rememberMe: boolean
}

/** 新建账号请求；displayName 为空时由后端归一化为 null。 */
export interface UserCreateInput {
  username: string
  password: string
  displayName: string | null
}

/** 账号修改请求；两个字段至少提供一个。 */
export interface UserPatchInput {
  displayName?: string | null
  status?: string
}
