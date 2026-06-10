import { useEffect, useRef, useState } from 'react'
import type { Profile } from '../../lib/api/profile'
import {
  useDeleteProfilePhoto,
  useMyPhotos,
  useSetPrimaryPhoto,
  useUpdatePhotoOrder,
  useUploadProfilePhoto,
} from '../../lib/query/photos'

export const MAX_PROFILE_PHOTOS = 10

/** One photo as the carousel renders it, regardless of where it lives. */
export type GalleryPhoto = { id: string; src: string; isPrimary: boolean }

type Staged = { id: string; file: File; url: string; isPrimary: boolean }

export type PhotoGallery = {
  photos: GalleryPhoto[]
  canAdd: boolean
  /** At least one photo that counts toward activation (ready on the server, or staged). */
  hasPhoto: boolean
  /** A photo mutation is in flight — used to gate Continue and the per-photo controls. */
  busy: boolean
  uploading: boolean
  add: (files: FileList | null) => Promise<void>
  remove: (id: string) => void
  reorder: (orderedIds: string[]) => Promise<void>
  setPrimary: (id: string) => void
  /** Upload staged photos (in order, honouring the chosen primary). No-op when already
      server-backed. Call after the profile row exists. Does not clear the staged photos —
      call {@link discardStaged} once the step has advanced. */
  commit: () => Promise<void>
  /** Revoke + clear staged photos. Call after advancing the step so the carousel stays
      static during the commit, then hands over to the server list. */
  discardStaged: () => void
}

/**
 * The profile-photo model shared by onboarding and profile editing. When the profile
 * already exists, every operation is a server mutation against `profile/photos`. When it
 * doesn't yet (a brand-new member who hasn't filled name / "how you appear"), photos are
 * staged locally — add / remove / reorder / set-primary all work the same — and uploaded
 * by `commit()` once the profile is created on Continue. The caller renders one carousel
 * either way; only the backing differs.
 */
export function usePhotoGallery(profile: Profile | null, reportError: (message: string | null) => void): PhotoGallery {
  const hasProfile = profile !== null
  const photosQuery = useMyPhotos(hasProfile)
  const uploadPhoto = useUploadProfilePhoto()
  const deletePhoto = useDeleteProfilePhoto()
  const setPrimaryPhoto = useSetPrimaryPhoto()
  const updatePhotoOrder = useUpdatePhotoOrder()

  const [staged, setStaged] = useState<Staged[]>([])
  const stagedRef = useRef(staged)
  useEffect(() => {
    stagedRef.current = staged
  }, [staged])
  // Revoke object URLs for any still-staged photos when the editor unmounts.
  useEffect(() => () => stagedRef.current.forEach((s) => URL.revokeObjectURL(s.url)), [])

  const serverList = photosQuery.data ?? []

  // Operate on staged photos whenever we have any (or no profile exists yet). This keeps
  // them displayed unchanged through the Continue-commit upload — the parent's profile
  // refetch flips `hasProfile` mid-upload, and without this the carousel would swap to the
  // still-loading server list and visibly repaint photo-by-photo. Staged photos give way
  // to the server list only once they've been uploaded and discarded.
  const staging = !hasProfile || staged.length > 0

  const photos: GalleryPhoto[] = staging
    ? staged.map((s) => ({ id: s.id, src: s.url, isPrimary: s.isPrimary }))
    : (() => {
        const primaryId = serverList.find((p) => p.isPrimary)?.id ?? serverList[0]?.id
        return serverList.map((p) => ({ id: p.id, src: p.thumbUrl || p.displayUrl, isPrimary: p.id === primaryId }))
      })()

  const count = photos.length
  const busy = uploadPhoto.isPending || updatePhotoOrder.isPending || setPrimaryPhoto.isPending || deletePhoto.isPending

  const add = async (files: FileList | null) => {
    const room = MAX_PROFILE_PHOTOS - count
    const picked = Array.from(files ?? []).slice(0, Math.max(0, room))
    if (picked.length === 0 || uploadPhoto.isPending) {
      return
    }
    reportError(null)

    if (!staging) {
      try {
        for (const file of picked) {
          await uploadPhoto.mutateAsync(file)
        }
      } catch (e) {
        reportError((e as Error).message)
      }
      return
    }

    // Staged: the first photo ever added is primary by default.
    setStaged((prev) => {
      const next = [
        ...prev,
        ...picked.map((file) => ({ id: crypto.randomUUID(), file, url: URL.createObjectURL(file), isPrimary: false })),
      ]
      if (!next.some((s) => s.isPrimary)) {
        next[0].isPrimary = true
      }
      return next
    })
  }

  const remove = (id: string) => {
    if (!staging) {
      deletePhoto.mutate(id)
      return
    }
    setStaged((prev) => {
      const removed = prev.find((s) => s.id === id)
      if (removed) {
        URL.revokeObjectURL(removed.url)
      }
      const next = prev.filter((s) => s.id !== id)
      if (removed?.isPrimary && next.length > 0 && !next.some((s) => s.isPrimary)) {
        next[0] = { ...next[0], isPrimary: true }
      }
      return next
    })
  }

  const reorder = async (orderedIds: string[]) => {
    if (!staging) {
      reportError(null)
      try {
        await updatePhotoOrder.mutateAsync(orderedIds)
      } catch (e) {
        reportError((e as Error).message)
      }
      return
    }
    setStaged((prev) => orderedIds.flatMap((id) => {
      const photo = prev.find((s) => s.id === id)
      return photo ? [photo] : []
    }))
  }

  const setPrimary = (id: string) => {
    if (!staging) {
      if (!setPrimaryPhoto.isPending) {
        setPrimaryPhoto.mutate(id)
      }
      return
    }
    setStaged((prev) => prev.map((s) => ({ ...s, isPrimary: s.id === id })))
  }

  const commit = async () => {
    const toUpload = stagedRef.current
    if (toUpload.length === 0) {
      return
    }
    // Upload in display order; the server marks position 0 primary, so only re-set primary
    // when the user chose a different one. Staged photos stay displayed until discardStaged,
    // so the page doesn't repaint while this runs.
    const uploaded = new Map<string, string>()
    for (const s of toUpload) {
      const photo = await uploadPhoto.mutateAsync(s.file)
      uploaded.set(s.id, photo.id)
    }
    const chosenPrimary = toUpload.find((s) => s.isPrimary) ?? toUpload[0]
    const primaryServerId = uploaded.get(chosenPrimary.id)
    if (primaryServerId && uploaded.get(toUpload[0].id) !== primaryServerId) {
      await setPrimaryPhoto.mutateAsync(primaryServerId)
    }
  }

  // Drop staged photos after they've been committed and the step has advanced — keeping
  // the carousel static during the upload, then handing display over to the server list.
  const discardStaged = () => {
    stagedRef.current.forEach((s) => URL.revokeObjectURL(s.url))
    setStaged([])
  }

  return {
    photos,
    canAdd: count < MAX_PROFILE_PHOTOS,
    hasPhoto: staging ? staged.length > 0 : serverList.some((p) => p.status === 'ready'),
    busy,
    uploading: uploadPhoto.isPending,
    add,
    remove,
    reorder,
    setPrimary,
    commit,
    discardStaged,
  }
}
