import type { components } from './generated/schema'
import { apiFetch } from './client'

export type NotificationSummary = components['schemas']['GetNotificationSummaryResponse']
export type NotificationItem = components['schemas']['NotificationItemResponse']

export async function getNotificationSummary(): Promise<NotificationSummary> {
  const response = await apiFetch('/notifications/summary')
  if (!response.ok) {
    throw new Error(`Could not load notifications (${response.status}).`)
  }
  return (await response.json()) as NotificationSummary
}

export async function markNotificationsSeen(): Promise<void> {
  const response = await apiFetch('/notifications/seen', { method: 'POST' })
  if (!response.ok) {
    throw new Error(`Could not update notifications (${response.status}).`)
  }
}
