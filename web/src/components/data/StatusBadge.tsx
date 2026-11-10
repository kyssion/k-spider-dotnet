import { getChannelStatus, getNodeStatus, getTaskStatus } from '../../lib/status'
import { Badge } from '../ui/Badge'

export type StatusKind = 'task' | 'channel' | 'node'

export interface StatusBadgeProps {
  kind: StatusKind
  status: string
}

export function StatusBadge({ kind, status }: StatusBadgeProps) {
  const meta =
    kind === 'task'
      ? getTaskStatus(status)
      : kind === 'channel'
        ? getChannelStatus(status)
        : getNodeStatus(status)

  return <Badge tone={meta.tone}>{meta.label}</Badge>
}
