import type { components } from './generated/schema'
import { apiFetch } from './client'

export type SubmitReportResponse = components['schemas']['SubmitReportResponse']

/**
 * Report categories (backbone §7.1 moderation.report_type). Kept here as the
 * user-facing labels; the wire value is the snake_case slug the API expects.
 */
export const REPORT_TYPES: { value: string; label: string }[] = [
  { value: 'inappropriate_content', label: 'Inappropriate content' },
  { value: 'harassment', label: 'Harassment' },
  { value: 'nsfw', label: 'Nudity or sexual content' },
  { value: 'fake_profile', label: 'Fake profile' },
  { value: 'ai_generated', label: 'AI-generated photos' },
  { value: 'stolen_photos', label: 'Stolen photos' },
  { value: 'spam', label: 'Spam' },
  { value: 'underage', label: 'Appears underage' },
]

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function submitReport(input: {
  targetProfileId: string
  type: string
  note?: string | null
}): Promise<SubmitReportResponse> {
  const response = await apiFetch('/reports', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      targetProfileId: input.targetProfileId,
      type: input.type,
      note: input.note ?? null,
    } satisfies components['schemas']['SubmitReportRequest']),
  })
  if (!response.ok) {
    throw await readError(response, `Could not send your report (${response.status}).`)
  }
  return (await response.json()) as SubmitReportResponse
}
