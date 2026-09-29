/** 与服务端 DTO 对应的类型 ( JSON 已是 camelCase ) */

export interface PageResult<T> {
  total: number
  page: number
  pageSize: number
  items: T[]
}

export interface NewsListItem {
  id: number
  fromMedia: number | null
  newsUrl: string | null
  newsTitle: string | null
  newsSummary: string | null
  newsFrom: string | null
  newsTime: string | null
  category: number
  downloadStatusCode: number
  failCount: number
  isPaid?: boolean
}

export interface NewsDetail {
  list: NewsListItem
  origin: {
    newsOriginContent: string | null
    newsOriginType: number
    status: number
    message: string | null
  } | null
  content: {
    newsTitle: string | null
    newsSummary: string | null
    newsKeyword: string | null
    newsContentText: string | null
    newsContentJson: string | null
  } | null
  images: { imageResourceUrl: string | null; imageName: string | null }[]
}

export interface FlashItem {
  id: number
  fromMedia: number
  category: number
  newsUrl: string
  newsTime: string
  title: string | null
  content: string | null
  keyword: string | null
  level: number
  stockList: string | null
}

export interface JobState {
  nodeId: string
  jobName: string
  nextFireTime: string | null
  isPaused: boolean
  lastFiredAt: string | null
  lastDurationMs: number | null
  lastSuccess: boolean | null
  consecutiveFailures: number
  lastError: string | null
  lastStats: string | null
  updateTime: string
}

export interface PipelineStatus {
  source: number
  statusCounts: Record<string, number>
  oldestPending: string | null
}

export interface FlashLag {
  source: number
  sourceName: string
  newest: string | null
  lagSeconds: number | null
}

/** 节点快照 payload ( spider_node_status.payload 的 JSON 结构 ) */
export interface NodePayload {
  apiAliveErrors: string[]
  pipelineBacklog: { source: number; statusCounts: Record<string, number> }[]
  flashLag: { source: number; newest: string; lagSeconds: number }[]
  verifyBlocked: {
    host: string
    kind: string
    until: string
    reason: string
  }[]
}

export interface NodeStatus {
  nodeId: string
  reportTime: string
  payload: string | null
}

export interface VolumeRow {
  bucket: string
  fromMedia: string
  count: number
}

export interface CountRow {
  key: string
  count: number
}

export interface VolumeDto {
  news: VolumeRow[]
  flash: VolumeRow[]
}

export interface DistributeDto {
  newsSource: CountRow[]
  newsCategory: CountRow[]
  flashSource: CountRow[]
  flashLevel: CountRow[]
}

export interface JobCommand {
  id: number
  nodeId: string | null
  jobName: string
  action: string
  status: 'pending' | 'done' | 'rejected'
  result: string | null
  consumedAt: string | null
  createTime: string
}
