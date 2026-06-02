import type { components } from './generated/schema'
import { apiFetch } from './client'

export type VisibilityPreferences = components['schemas']['VisibilityPreferencesResponse']
export type VisibilityInput = components['schemas']['UpdateVisibilitySettingsRequest']
export type LocationInput = components['schemas']['UpdateLocationRequest']
export type LocationState = components['schemas']['LocationResponse']
export type BlockedProfile = components['schemas']['BlockedProfileResponse']

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function updateVisibility(input: VisibilityInput): Promise<VisibilityPreferences> {
  const response = await apiFetch('/settings/visibility', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw await readError(response, `Could not save visibility settings (${response.status}).`)
  }
  return (await response.json()) as VisibilityPreferences
}

export async function updateLocation(input: LocationInput): Promise<LocationState> {
  const response = await apiFetch('/settings/location', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw await readError(response, `Could not save your location (${response.status}).`)
  }
  return (await response.json()) as LocationState
}

export async function listBlocks(): Promise<BlockedProfile[]> {
  const response = await apiFetch('/settings/blocks')
  if (!response.ok) {
    throw new Error(`Could not load your blocked list (${response.status}).`)
  }
  return (await response.json()) as BlockedProfile[]
}

export async function blockProfile(targetProfileId: string): Promise<void> {
  const response = await apiFetch('/settings/blocks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ targetProfileId } satisfies components['schemas']['BlockUserRequest']),
  })
  if (!response.ok) {
    throw await readError(response, `Could not block this profile (${response.status}).`)
  }
}

export async function unblockProfile(profileId: string): Promise<void> {
  const response = await apiFetch(`/settings/blocks/${profileId}`, { method: 'DELETE' })
  if (!response.ok) {
    throw await readError(response, `Could not unblock this profile (${response.status}).`)
  }
}

export async function requestAccountDeletion(): Promise<components['schemas']['AccountDeletionResponse']> {
  const response = await apiFetch('/settings/account/delete', { method: 'POST' })
  if (!response.ok) {
    throw await readError(response, `Could not start account deletion (${response.status}).`)
  }
  return (await response.json()) as components['schemas']['AccountDeletionResponse']
}

export async function exportAccount(): Promise<Blob> {
  const response = await apiFetch('/settings/account/export')
  if (!response.ok) {
    throw await readError(response, `Could not export your data (${response.status}).`)
  }
  return response.blob()
}
