import type { components } from './generated/schema'
import { apiFetch } from './client'

export type AppreciationCategory = components['schemas']['AppreciationCategoryDto']
export type AppreciationStyle = components['schemas']['GetAppreciationStyleResponse']
export type ReceivedAppreciation = components['schemas']['GetReceivedAppreciationResponse']
export type SubmitAppreciationRequest = components['schemas']['SubmitAppreciationRequest']
export type SubmitAppreciationResponse = components['schemas']['SubmitAppreciationResponse']

/**
 * An API failure that carries the HTTP status and the RFC 9457 problem `type`
 * slug, so callers can tell a *permanent* rejection (the receiver is gone or the
 * card was already appreciated — retrying this card will never succeed) from a
 * transient/validation one worth retrying. `problem` is the trailing segment of
 * the problem type (e.g. "profile-unavailable").
 */
export class ApiError extends Error {
  readonly status: number
  readonly problem: string | null

  constructor(message: string, status: number, problem: string | null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function readError(response: Response, fallback: string): Promise<ApiError> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string; type?: string }
    const problem = body.type ? (body.type.split('/').pop() ?? null) : null
    return new ApiError(body.detail ?? body.title ?? fallback, response.status, problem)
  } catch {
    return new ApiError(fallback, response.status, null)
  }
}

export async function getAppreciationCategories(): Promise<AppreciationCategory[]> {
  const response = await apiFetch('/appreciation-categories')
  if (!response.ok) {
    throw await readError(response, `Could not load appreciation categories (${response.status}).`)
  }

  return (await response.json()) as AppreciationCategory[]
}

export async function getReceivedAppreciation(includeEvents = true): Promise<ReceivedAppreciation> {
  const query = includeEvents ? '?includeEvents=true' : ''
  const response = await apiFetch(`/appreciations/received${query}`)
  if (!response.ok) {
    throw await readError(response, `Could not load received appreciation (${response.status}).`)
  }

  return (await response.json()) as ReceivedAppreciation
}

export async function getAppreciationStyle(): Promise<AppreciationStyle> {
  const response = await apiFetch('/appreciation-style/me')
  if (!response.ok) {
    throw await readError(response, `Could not load appreciation style (${response.status}).`)
  }

  return (await response.json()) as AppreciationStyle
}

export async function submitAppreciation(
  request: SubmitAppreciationRequest,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<SubmitAppreciationResponse> {
  const response = await apiFetch('/appreciations', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey,
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw await readError(response, `Could not save appreciation (${response.status}).`)
  }

  return (await response.json()) as SubmitAppreciationResponse
}
