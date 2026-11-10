export type StatusTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger' | 'running'

export interface StatusMeta {
  label: string
  tone: StatusTone
}

const TASK_STATUS: Record<string, StatusMeta> = {
  pushing: { label: '推送中', tone: 'info' },
  push_failed: { label: '推送失败', tone: 'warning' },
  pushed: { label: '已推送', tone: 'info' },
  running: { label: '运行中', tone: 'running' },
  run_failed: { label: '运行失败', tone: 'danger' },
  success: { label: '成功', tone: 'success' },
  abandoned: { label: '已放弃', tone: 'neutral' },
  cancelled: { label: '已取消', tone: 'neutral' },
}

const CHANNEL_STATUS: Record<string, StatusMeta> = {
  active: { label: '启用', tone: 'success' },
  paused: { label: '暂停', tone: 'warning' },
  retired: { label: '退役', tone: 'neutral' },
}

const NODE_STATUS: Record<string, StatusMeta> = {
  online: { label: '在线', tone: 'success' },
  offline: { label: '离线', tone: 'neutral' },
}

export function getTaskStatus(status: string): StatusMeta {
  return TASK_STATUS[status] ?? { label: status, tone: 'neutral' }
}

export function getChannelStatus(status: string): StatusMeta {
  return CHANNEL_STATUS[status] ?? { label: status, tone: 'neutral' }
}

export function getNodeStatus(status: string): StatusMeta {
  return NODE_STATUS[status] ?? { label: status, tone: 'neutral' }
}
