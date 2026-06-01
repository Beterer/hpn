import type { components } from './generated/schema'
import { apiFetch } from './client'

export type FeedProfile = components['schemas']['FeedProfileDto']
export type FeedPhoto = components['schemas']['FeedPhotoDto']

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

/**
 * The next batch of eligible profiles (backbone §6.5, §9.2). The client drives
 * session-level dedupe by passing already-seen profile ids — there is no
 * server-side impressions table (§7.6).
 */
export async function getFeedNext(options?: { limit?: number; seen?: string[] }): Promise<FeedProfile[]> {
  const params = new URLSearchParams()
  if (options?.limit) {
    params.set('limit', String(options.limit))
  }
  if (options?.seen?.length) {
    params.set('seen', options.seen.join(','))
  }

  const query = params.toString()
  const response = await apiFetch(`/feed/next${query ? `?${query}` : ''}`)
  if (!response.ok) {
    throw await readError(response, `Could not load the feed (${response.status}).`)
  }

  return (await response.json()) as FeedProfile[]
}
