import type { components } from './generated/schema'
import { apiFetch } from './client'

export type AppreciationCategory = components['schemas']['AppreciationCategoryDto']
export type SubmitAppreciationRequest = components['schemas']['SubmitAppreciationRequest']
export type SubmitAppreciationResponse = components['schemas']['SubmitAppreciationResponse']

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function getAppreciationCategories(): Promise<AppreciationCategory[]> {
  const response = await apiFetch('/appreciation-categories')
  if (!response.ok) {
    throw await readError(response, `Could not load appreciation categories (${response.status}).`)
  }

  return (await response.json()) as AppreciationCategory[]
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
