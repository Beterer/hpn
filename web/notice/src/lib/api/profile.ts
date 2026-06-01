import type { components } from './generated/schema'
import { apiFetch } from './client'

export type Interest = components['schemas']['InterestResponse']
export type Profile = components['schemas']['ProfileResponse']
export type PublicProfile = components['schemas']['PublicProfileResponse']
export type UpsertProfileInput = components['schemas']['UpsertProfileRequest']

async function readJson<T>(response: Response): Promise<T> {
  return (await response.json()) as T
}

export async function getMyProfile(): Promise<Profile | null> {
  const response = await apiFetch('/profile/me')
  if (response.status === 404) {
    return null
  }
  if (!response.ok) {
    throw new Error(`Could not load your profile (${response.status}).`)
  }
  return readJson<Profile>(response)
}

export async function upsertProfile(input: UpsertProfileInput): Promise<Profile> {
  const response = await apiFetch('/profile', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw new Error(`Could not save your profile (${response.status}).`)
  }
  return readJson<Profile>(response)
}

export async function getInterests(): Promise<Interest[]> {
  const response = await apiFetch('/interests')
  if (!response.ok) {
    throw new Error(`Could not load interests (${response.status}).`)
  }
  return readJson<Interest[]>(response)
}

export async function updateProfileInterests(interestIds: string[]): Promise<Profile> {
  const response = await apiFetch('/profile/interests', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ interestIds } satisfies components['schemas']['UpdateProfileInterestsRequest']),
  })
  if (!response.ok) {
    throw new Error(`Could not save interests (${response.status}).`)
  }
  return readJson<Profile>(response)
}

export async function updateProfileStatus(status: 'active' | 'paused'): Promise<Profile> {
  const response = await apiFetch('/profile/status', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status } satisfies components['schemas']['UpdateProfileStatusRequest']),
  })
  if (!response.ok) {
    throw new Error(`Could not update profile status (${response.status}).`)
  }
  return readJson<Profile>(response)
}

export async function getPublicProfile(id: string): Promise<PublicProfile | null> {
  const response = await apiFetch(`/profiles/${id}`)
  if (response.status === 404) {
    return null
  }
  if (!response.ok) {
    throw new Error(`Could not load profile (${response.status}).`)
  }
  return readJson<PublicProfile>(response)
}
