import { getChannelStatus, getNodeStatus, getTaskStatus } from '../../lib/status'
import { Badge } from '../ui/Badge'

/** 状态来源：任务/渠道/节点。 */
export type StatusKind = 'task' | 'channel' | 'node'

/** 状态徽标属性。 */
export interface StatusBadgeProps {
  kind: StatusKind
  status: string
}

/** 根据状态域选择映射，避免页面重复写状态文案。 */
export function StatusBadge({ kind, status }: StatusBadgeProps) {
  const meta =
    kind === 'task'
      ? getTaskStatus(status)
      : kind === 'channel'
        ? getChannelStatus(status)
        : getNodeStatus(status)

  return <Badge tone={meta.tone}>{meta.label}</Badge>
}
