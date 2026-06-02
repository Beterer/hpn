import type { components } from './generated/schema'
import { apiFetch } from './client'

export type SocialFingerprint = components['schemas']['GetMyFingerprintResponse']

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function getMyFingerprint(): Promise<SocialFingerprint> {
  const response = await apiFetch('/fingerprint/me')
  if (!response.ok) {
    throw await readError(response, `Could not load fingerprint (${response.status}).`)
  }

  return (await response.json()) as SocialFingerprint
}
