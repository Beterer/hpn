import type { components } from './generated/schema'
import { apiFetch } from './client'

export type AdminQueueItem = components['schemas']['AdminQueueItemResponse']
export type AdminReport = components['schemas']['AdminReportResponse']
export type AdminStats = components['schemas']['AdminStatsResponse']
export type ApplyProfileActionRequest = components['schemas']['ApplyProfileActionRequest']
export type ApplyProfileActionResponse = components['schemas']['ApplyProfileActionResponse']
export type ResolveAppealRequest = components['schemas']['ResolveAppealRequest']
export type ResolveAppealResponse = components['schemas']['ResolveAppealResponse']

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function getAdminQueue(limit = 25): Promise<AdminQueueItem[]> {
  const response = await apiFetch(`/admin/queue?limit=${limit}`)
  if (!response.ok) {
    throw await readError(response, `Could not load the queue (${response.status}).`)
  }
  return (await response.json()) as AdminQueueItem[]
}

export async function getAdminReports(input: { status?: string; limit?: number } = {}): Promise<AdminReport[]> {
  const params = new URLSearchParams()
  if (input.status && input.status !== 'all') {
    params.set('status', input.status)
  }
  params.set('limit', String(input.limit ?? 50))

  const response = await apiFetch(`/admin/reports?${params.toString()}`)
  if (!response.ok) {
    throw await readError(response, `Could not load reports (${response.status}).`)
  }
  return (await response.json()) as AdminReport[]
}

export async function getAdminStats(): Promise<AdminStats> {
  const response = await apiFetch('/admin/stats')
  if (!response.ok) {
    throw await readError(response, `Could not load stats (${response.status}).`)
  }
  return (await response.json()) as AdminStats
}

export async function applyProfileAction(
  profileId: string,
  input: ApplyProfileActionRequest,
): Promise<ApplyProfileActionResponse> {
  const response = await apiFetch(`/admin/profiles/${profileId}/action`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw await readError(response, `Could not apply action (${response.status}).`)
  }
  return (await response.json()) as ApplyProfileActionResponse
}

export async function resolveAppeal(
  appealId: string,
  input: ResolveAppealRequest,
): Promise<ResolveAppealResponse> {
  const response = await apiFetch(`/admin/appeals/${appealId}/resolve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw await readError(response, `Could not resolve appeal (${response.status}).`)
  }
  return (await response.json()) as ResolveAppealResponse
}
