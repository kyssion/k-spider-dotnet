/** 状态色调只表达语义；具体颜色由 Badge 的 tone 映射决定。 */
export type StatusTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger' | 'running'

/** 状态展示元数据：文案 + 色调。 */
export interface StatusMeta {
  label: string
  tone: StatusTone
}

/**
 * 任务状态映射。
 *
 * 与 admin 的 sys_task.status 一一对应；新增状态时必须同时更新这里与后端状态机。
 */
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

/** 渠道状态映射；retired 是终态，不允许恢复。 */
const CHANNEL_STATUS: Record<string, StatusMeta> = {
  active: { label: '启用', tone: 'success' },
  paused: { label: '暂停', tone: 'warning' },
  retired: { label: '退役', tone: 'neutral' },
}

/** 节点镜像状态；实时在线判定以 admin 内存心跳为准。 */
const NODE_STATUS: Record<string, StatusMeta> = {
  online: { label: '在线', tone: 'success' },
  offline: { label: '离线', tone: 'neutral' },
}

/**
 * 读取任务状态展示信息。
 *
 * 未知状态回退为原始值，避免前端因后端新增状态直接白屏；便于排障时看到原始编码。
 * @param status 数据库状态值。
 * @returns 展示文案与色调。
 */
export function getTaskStatus(status: string): StatusMeta {
  return TASK_STATUS[status] ?? { label: status, tone: 'neutral' }
}

/**
 * 读取渠道状态展示信息；未知状态同样回退原始值。
 * @param status 渠道状态值。
 * @returns 展示文案与色调。
 */
export function getChannelStatus(status: string): StatusMeta {
  return CHANNEL_STATUS[status] ?? { label: status, tone: 'neutral' }
}

/**
 * 读取节点状态展示信息。
 * @param status 节点状态值。
 * @returns 展示文案与色调。
 */
export function getNodeStatus(status: string): StatusMeta {
  return NODE_STATUS[status] ?? { label: status, tone: 'neutral' }
}
