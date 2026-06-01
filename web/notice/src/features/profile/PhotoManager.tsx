import { useEffect, useMemo, useState } from 'react'
import type { Photo } from '../../lib/api/photos'
import {
  useDeleteProfilePhoto,
  useMyPhotos,
  useUpdatePhotoOrder,
  useUploadProfilePhoto,
} from '../../lib/query/photos'

type PhotoManagerProps = {
  enabled?: boolean
  compact?: boolean
}

export function PhotoManager({ enabled = true, compact = false }: PhotoManagerProps) {
  const photosQuery = useMyPhotos(enabled)
  const uploadMutation = useUploadProfilePhoto()
  const orderMutation = useUpdatePhotoOrder()
  const deleteMutation = useDeleteProfilePhoto()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [zoom, setZoom] = useState(1)
  const [localError, setLocalError] = useState<string | null>(null)

  const photos = useMemo(
    () => [...(photosQuery.data ?? [])].sort((a, b) => Number(a.position) - Number(b.position)),
    [photosQuery.data],
  )

  const busy = uploadMutation.isPending || orderMutation.isPending || deleteMutation.isPending
  const maxReached = photos.length >= 5

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl)
    }
  }, [previewUrl])

  function chooseFile(file: File | null) {
    setLocalError(null)
    setSelectedFile(null)
    setZoom(1)
    if (previewUrl) URL.revokeObjectURL(previewUrl)
    setPreviewUrl(null)

    if (!file) return
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      setLocalError('Choose a JPEG, PNG, or WebP image.')
      return
    }

    setSelectedFile(file)
    setPreviewUrl(URL.createObjectURL(file))
  }

  async function uploadSelected() {
    if (!selectedFile) return
    setLocalError(null)
    try {
      const cropped = await cropToSquareFile(selectedFile, zoom)
      await uploadMutation.mutateAsync(cropped)
      chooseFile(null)
    } catch (error) {
      setLocalError(error instanceof Error ? error.message : 'Photo could not be prepared.')
    }
  }

  function movePhoto(photo: Photo, direction: -1 | 1) {
    const index = photos.findIndex((item) => item.id === photo.id)
    const nextIndex = index + direction
    if (index < 0 || nextIndex < 0 || nextIndex >= photos.length) return

    const next = [...photos]
    const [item] = next.splice(index, 1)
    next.splice(nextIndex, 0, item)
    orderMutation.mutate(next.map((entry) => entry.id))
  }

  return (
    <div className="grid gap-5">
      <div>
        <h2 className={`${compact ? 'text-xl' : 'text-2xl'} font-semibold text-zinc-950`}>Photos</h2>
        <p className="mt-1 text-sm leading-6 text-zinc-600">
          Add at least one photo to activate. Notice strips metadata before storing processed WebP versions.
        </p>
      </div>

      {photosQuery.isLoading && <p className="text-sm text-zinc-500">Loading photos…</p>}
      {photosQuery.isError && <p className="text-sm text-rose-700">Photos could not be loaded.</p>}

      {!photosQuery.isLoading && (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {photos.map((photo, index) => (
            <div key={photo.id} className="grid gap-3 rounded-lg border border-zinc-200 bg-white p-3">
              <div className="relative aspect-square overflow-hidden rounded-md bg-zinc-100">
                <img
                  src={photo.thumbUrl}
                  alt={index === 0 ? 'Primary profile upload' : 'Profile upload'}
                  className="h-full w-full object-cover"
                />
                {index === 0 && (
                  <span className="absolute left-2 top-2 rounded-md bg-zinc-950 px-2 py-1 text-xs font-medium text-white">
                    Primary
                  </span>
                )}
              </div>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  disabled={busy || index === 0}
                  onClick={() => movePhoto(photo, -1)}
                  className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-700 hover:border-teal-700 hover:text-teal-800 disabled:opacity-50"
                >
                  Move up
                </button>
                <button
                  type="button"
                  disabled={busy || index === photos.length - 1}
                  onClick={() => movePhoto(photo, 1)}
                  className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-700 hover:border-teal-700 hover:text-teal-800 disabled:opacity-50"
                >
                  Move down
                </button>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => deleteMutation.mutate(photo.id)}
                  className="rounded-md border border-rose-200 px-3 py-1.5 text-sm font-medium text-rose-700 hover:border-rose-500 disabled:opacity-50"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {!maxReached && (
        <div className="grid gap-4 rounded-lg border border-dashed border-zinc-300 bg-zinc-50 p-4">
          <label className="grid gap-2 text-sm font-medium text-zinc-800">
            Upload photo
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(event) => chooseFile(event.target.files?.[0] ?? null)}
              className="block w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-700 file:mr-3 file:rounded-md file:border-0 file:bg-zinc-950 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-white"
            />
          </label>

          {previewUrl && (
            <div className="grid gap-4 sm:grid-cols-[180px_1fr] sm:items-start">
              <div className="aspect-square overflow-hidden rounded-lg bg-zinc-200">
                <img
                  src={previewUrl}
                  alt="Selected crop preview"
                  className="h-full w-full object-cover"
                  style={{ transform: `scale(${zoom})` }}
                />
              </div>
              <div className="grid gap-3">
                <label className="grid gap-2 text-sm font-medium text-zinc-800">
                  Crop zoom
                  <input
                    type="range"
                    min="1"
                    max="2"
                    step="0.05"
                    value={zoom}
                    onChange={(event) => setZoom(Number(event.target.value))}
                    className="w-full accent-teal-700"
                  />
                </label>
                <button
                  type="button"
                  disabled={!selectedFile || uploadMutation.isPending}
                  onClick={uploadSelected}
                  className="w-fit rounded-lg bg-zinc-950 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-800 disabled:opacity-60"
                >
                  {uploadMutation.isPending ? 'Uploading…' : 'Use this crop'}
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {maxReached && <p className="text-sm text-zinc-500">You have the five-photo limit for this version.</p>}
      {(localError || uploadMutation.error || orderMutation.error || deleteMutation.error) && (
        <p className="text-sm text-rose-700">
          {localError ??
            uploadMutation.error?.message ??
            orderMutation.error?.message ??
            deleteMutation.error?.message}
        </p>
      )}
    </div>
  )
}

async function cropToSquareFile(file: File, zoom: number): Promise<File> {
  const bitmap = await createImageBitmap(file)
  const side = Math.min(bitmap.width, bitmap.height) / zoom
  const sourceX = (bitmap.width - side) / 2
  const sourceY = (bitmap.height - side) / 2
  const canvas = document.createElement('canvas')
  canvas.width = 1200
  canvas.height = 1200
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Photo preview could not be prepared.')

  context.drawImage(bitmap, sourceX, sourceY, side, side, 0, 0, canvas.width, canvas.height)
  bitmap.close()

  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.92))
  if (!blob) throw new Error('Photo preview could not be prepared.')

  return new File([blob], file.name.replace(/\.[^.]+$/, '.jpg'), { type: 'image/jpeg' })
}
