import type { components } from './generated/schema'
import { apiFetch } from './client'

export type AuthUser = components['schemas']['AuthUserDto']
export type Me = components['schemas']['MeResponse']
export type GuestSession = components['schemas']['StartGuestSessionResponse']

/**
 * Request a magic sign-in link. The API always answers 202 regardless of whether
 * the account exists (no enumeration — backbone §10.1), so success here only
 * means "we accepted the request".
 */
export async function requestMagicLink(email: string): Promise<void> {
  const response = await apiFetch('/auth/magic-link', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email } satisfies components['schemas']['RequestMagicLinkRequest']),
  })
  if (!response.ok) {
    throw new Error(`Could not request a sign-in link (${response.status}).`)
  }
}

/** Exchange the emailed token for a session cookie; returns the signed-in user. */
export async function verifyMagicLink(token: string): Promise<AuthUser> {
  const response = await apiFetch('/auth/verify', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token } satisfies components['schemas']['VerifyMagicLinkRequest']),
  })
  if (!response.ok) {
    throw new Error('This sign-in link is no longer valid. Request a new one.')
  }
  return (await response.json()) as AuthUser
}

export async function startGuestSession(): Promise<GuestSession> {
  const response = await apiFetch('/guest/start', { method: 'POST' })
  if (!response.ok) {
    throw new Error(`Could not start browsing (${response.status}).`)
  }
  return (await response.json()) as GuestSession
}

/** Current account + onboarding state, or null when not signed in as a member. */
export async function getMe(): Promise<Me | null> {
  const response = await apiFetch('/me')
  if (response.status === 401 || response.status === 403) {
    return null
  }
  if (!response.ok) {
    throw new Error(`Could not load your account (${response.status}).`)
  }
  return (await response.json()) as Me
}

/** Revoke the current session and clear the cookie. */
export async function logout(): Promise<void> {
  const response = await apiFetch('/auth/logout', { method: 'POST' })
  if (!response.ok && response.status !== 401) {
    throw new Error(`Could not sign out (${response.status}).`)
  }
}
