import type { components } from './generated/schema'
import { apiFetch } from './client'

export type Photo = components['schemas']['PhotoResponse']
export type UpdatePhotoOrderInput = components['schemas']['UpdatePhotoOrderRequest']

async function readJson<T>(response: Response): Promise<T> {
  return (await response.json()) as T
}

async function readError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as { detail?: string; title?: string }
    return new Error(body.detail ?? body.title ?? fallback)
  } catch {
    return new Error(fallback)
  }
}

export async function getMyPhotos(): Promise<Photo[]> {
  const response = await apiFetch('/profile/photos')
  if (response.status === 404) {
    return []
  }
  if (!response.ok) {
    throw await readError(response, `Could not load photos (${response.status}).`)
  }
  return readJson<Photo[]>(response)
}

export async function uploadProfilePhoto(file: File): Promise<Photo> {
  const form = new FormData()
  form.append('file', file)

  const response = await apiFetch('/profile/photos', {
    method: 'POST',
    body: form,
  })
  if (!response.ok) {
    throw await readError(response, `Could not upload photo (${response.status}).`)
  }
  return readJson<Photo>(response)
}

export async function updatePhotoOrder(photoIds: string[]): Promise<Photo[]> {
  const response = await apiFetch('/profile/photos/order', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ photoIds } satisfies UpdatePhotoOrderInput),
  })
  if (!response.ok) {
    throw await readError(response, `Could not reorder photos (${response.status}).`)
  }
  return readJson<Photo[]>(response)
}

export async function deleteProfilePhoto(photoId: string): Promise<void> {
  const response = await apiFetch(`/profile/photos/${photoId}`, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw await readError(response, `Could not delete photo (${response.status}).`)
  }
}
