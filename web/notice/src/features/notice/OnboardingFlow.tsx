import { useEffect, useLayoutEffect, useMemo, useRef, useState, type PointerEvent } from 'react'
import { createPortal } from 'react-dom'
import type { Profile } from '../../lib/api/profile'
import { useInterests, useUpdateProfileInterests, useUpdateProfileStatus, useUpsertProfile } from '../../lib/query/profile'
import {
  useDeleteProfilePhoto,
  useMyPhotos,
  useSetPrimaryPhoto,
  useUpdatePhotoOrder,
  useUploadProfilePhoto,
} from '../../lib/query/photos'
import { useUpdateVisibility } from '../../lib/query/settings'
import { Wordmark } from './ui'

const GENDERS: { value: string; label: string }[] = [
  { value: 'woman', label: 'Woman' },
  { value: 'man', label: 'Man' },
  { value: 'non_binary', label: 'Non-binary' },
  { value: 'self_describe', label: 'Rather not say' },
]

const MAX_PROFILE_PHOTOS = 10

type Visibility = {
  womenForWomen: boolean
  hideFromCountry: boolean
  verifiedOnly: boolean
}

/**
 * Profile setup for a signed-in member without an active profile (ADR-025
 * redesign). Three steps — Basics, Interests, Privacy — persisting as it goes,
 * then activating. Leaves out age, height, body type, income, scores, and
 * public counts by construction (product principle).
 */
export function OnboardingFlow({ profile, onDone }: { profile: Profile | null; onDone: () => void }) {
  const hasExistingProfile = profile !== null
  const isEditing = profile !== null && profile.status !== 'draft'
  const upsert = useUpsertProfile()
  const updateInterests = useUpdateProfileInterests()
  const updateVisibility = useUpdateVisibility()
  const setStatus = useUpdateProfileStatus()
  const interestList = useInterests()
  const photos = useMyPhotos(true)
  const uploadPhoto = useUploadProfilePhoto()
  const deletePhoto = useDeleteProfilePhoto()
  const setPrimaryPhoto = useSetPrimaryPhoto()
  const updatePhotoOrder = useUpdatePhotoOrder()

  const [step, setStep] = useState(0)
  const [displayName, setDisplayName] = useState(profile?.displayName ?? '')
  const [gender, setGender] = useState(profile?.gender ?? '')
  const [selected, setSelected] = useState<Set<string>>(() => new Set(profile?.interests?.map((i) => i.id) ?? []))
  const [vis, setVis] = useState<Visibility>({
    womenForWomen: profile?.visibilityPreferences?.womenForWomen ?? false,
    hideFromCountry: profile?.visibilityPreferences?.hideFromCountry ?? false,
    verifiedOnly: profile?.visibilityPreferences?.verifiedOnly ?? false,
  })
  const [error, setError] = useState<string | null>(null)
  const [draggedPhotoId, setDraggedPhotoId] = useState<string | null>(null)
  const [dropIndex, setDropIndex] = useState<number | null>(null)
  const [dragPoint, setDragPoint] = useState<{ x: number; y: number } | null>(null)
  const photoRowRef = useRef<HTMLDivElement>(null)
  const revealPhotoIdRef = useRef<string | null>(null)
  const draggedPhotoIdRef = useRef<string | null>(null)
  const dropIndexRef = useRef<number | null>(null)
  const pointerXRef = useRef<number | null>(null)
  const autoScrollFrameRef = useRef<number | null>(null)

  const photoList = useMemo(() => photos.data ?? [], [photos.data])
  const primaryPhotoId = photoList.find((photo) => photo.isPrimary)?.id ?? photoList[0]?.id
  const draggedPhoto = photoList.find((photo) => photo.id === draggedPhotoId)
  const canAddPhotos = photoList.length < MAX_PROFILE_PHOTOS
  const photoCarousel: Array<
    { kind: 'photo'; photo: (typeof photoList)[number] } | { kind: 'add' }
  > = photoList.map((photo) => ({ kind: 'photo', photo }))
  if (canAddPhotos) {
    photoCarousel.push({ kind: 'add' })
  }

  useLayoutEffect(() => {
    const row = photoRowRef.current
    if (row && canAddPhotos) {
      row.scrollLeft = row.scrollWidth
    }
  }, [canAddPhotos, photoList.length])

  useLayoutEffect(() => {
    const row = photoRowRef.current
    const photo = revealPhotoIdRef.current === null
      ? null
      : [...(row?.querySelectorAll<HTMLElement>('[data-photo-id]') ?? [])]
        .find((slot) => slot.dataset.photoId === revealPhotoIdRef.current)

    if (photo) {
      photo.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' })
      revealPhotoIdRef.current = null
    }
  }, [photoList])

  useEffect(() => () => {
    if (autoScrollFrameRef.current !== null) {
      cancelAnimationFrame(autoScrollFrameRef.current)
    }
  }, [])

  // Mirror the backend activation requirement (a ready photo) so it's enforced on the
  // step that owns photos — not deferred to "Activate", which strands the user two
  // screens away from where they'd fix it.
  const hasReadyPhoto = photoList.some((p) => p.status === 'ready')

  const makePrimary = (photoId: string) => {
    if (setPrimaryPhoto.isPending) {
      return
    }
    setPrimaryPhoto.mutate(photoId)
  }

  const updateDropIndex = (clientX: number) => {
    const row = photoRowRef.current
    if (!row) {
      return
    }

    const slots = [...row.querySelectorAll<HTMLElement>('[data-photo-index]')]
    const nextIndex = slots.findIndex((slot) => clientX < slot.getBoundingClientRect().left + slot.offsetWidth / 2)
    const index = nextIndex === -1 ? photoList.length - 1 : Number(slots[nextIndex].dataset.photoIndex)
    dropIndexRef.current = index
    setDropIndex(index)
  }

  const runAutoScroll = () => {
    const row = photoRowRef.current
    const clientX = pointerXRef.current
    if (!row || clientX === null || draggedPhotoIdRef.current === null) {
      autoScrollFrameRef.current = null
      return
    }

    const bounds = row.getBoundingClientRect()
    const edge = Math.min(72, bounds.width * 0.24)
    let speed = 0
    if (clientX < bounds.left + edge) {
      speed = -Math.min(18, Math.ceil((bounds.left + edge - clientX) / 6))
    } else if (clientX > bounds.right - edge) {
      speed = Math.min(18, Math.ceil((clientX - (bounds.right - edge)) / 6))
    }

    if (speed !== 0) {
      row.scrollLeft += speed
      updateDropIndex(clientX)
    }
    autoScrollFrameRef.current = requestAnimationFrame(runAutoScroll)
  }

  const startPhotoDrag = (photoId: string, event: PointerEvent<HTMLDivElement>) => {
    if (updatePhotoOrder.isPending) {
      return
    }

    event.currentTarget.setPointerCapture(event.pointerId)
    draggedPhotoIdRef.current = photoId
    pointerXRef.current = event.clientX
    setDraggedPhotoId(photoId)
    setDragPoint({ x: event.clientX, y: event.clientY })
    updateDropIndex(event.clientX)
    autoScrollFrameRef.current = requestAnimationFrame(runAutoScroll)
  }

  const movePhotoDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (draggedPhotoIdRef.current === null) {
      return
    }
    pointerXRef.current = event.clientX
    setDragPoint({ x: event.clientX, y: event.clientY })
    updateDropIndex(event.clientX)
  }

  const finishPhotoDrag = async () => {
    const photoId = draggedPhotoIdRef.current
    const targetIndex = dropIndexRef.current
    draggedPhotoIdRef.current = null
    dropIndexRef.current = null
    pointerXRef.current = null
    setDraggedPhotoId(null)
    setDropIndex(null)
    setDragPoint(null)
    if (autoScrollFrameRef.current !== null) {
      cancelAnimationFrame(autoScrollFrameRef.current)
      autoScrollFrameRef.current = null
    }

    const sourceIndex = photoList.findIndex((photo) => photo.id === photoId)
    if (photoId === null || targetIndex === null || sourceIndex === targetIndex) {
      return
    }

    const reordered = [...photoList]
    const [selectedPhoto] = reordered.splice(sourceIndex, 1)
    reordered.splice(targetIndex, 0, selectedPhoto)

    setError(null)
    revealPhotoIdRef.current = photoId
    try {
      await updatePhotoOrder.mutateAsync(reordered.map((photo) => photo.id))
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const cancelPhotoDrag = () => {
    draggedPhotoIdRef.current = null
    dropIndexRef.current = null
    pointerXRef.current = null
    setDraggedPhotoId(null)
    setDropIndex(null)
    setDragPoint(null)
    if (autoScrollFrameRef.current !== null) {
      cancelAnimationFrame(autoScrollFrameRef.current)
      autoScrollFrameRef.current = null
    }
  }

  const addPhotos = async (files: FileList | null) => {
    const selectedFiles = Array.from(files ?? []).slice(0, MAX_PROFILE_PHOTOS - photoList.length)
    if (selectedFiles.length === 0 || uploadPhoto.isPending) {
      return
    }

    if (!hasExistingProfile) {
      if (!displayName.trim() || !gender) {
        setError('Add a display name and choose how you appear before uploading photos.')
        return
      }
      try {
        await upsert.mutateAsync({ displayName: displayName.trim(), gender, selfDescribeText: null })
      } catch (e) {
        setError((e as Error).message)
        return
      }
    }

    setError(null)
    try {
      for (const file of selectedFiles) {
        await uploadPhoto.mutateAsync(file)
      }
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const saveBasics = async () => {
    setError(null)
    if (!displayName.trim() || !gender) {
      setError('Add a display name and choose how you appear.')
      return
    }
    if (uploadPhoto.isPending) {
      setError('Hang on — your photo is still uploading.')
      return
    }
    if (!hasReadyPhoto) {
      setError('Add at least one photo so people can notice you.')
      return
    }
    try {
      await upsert.mutateAsync({
        displayName: displayName.trim(),
        gender,
        selfDescribeText: hasExistingProfile ? profile.selfDescribeText : null,
      })
      setStep(1)
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const saveInterests = async () => {
    setError(null)
    try {
      await updateInterests.mutateAsync([...selected])
      setStep(2)
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const activate = async () => {
    setError(null)
    try {
      await updateVisibility.mutateAsync({
        womenForWomen: vis.womenForWomen,
        hideFromCountry: vis.hideFromCountry,
        verifiedOnly: vis.verifiedOnly,
        paused: false,
        hiddenFromGuests: profile?.visibilityPreferences?.hiddenFromGuests ?? false,
        minDistanceKm: profile?.visibilityPreferences?.minDistanceKm == null ? null : Number(profile.visibilityPreferences.minDistanceKm),
      })
      await setStatus.mutateAsync('active')
      onDone()
    } catch (e) {
      setError((e as Error).message)
    }
  }

  const toggleInterest = (id: string) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else if (next.size < 6) {
        next.add(id)
      }
      return next
    })
  }

  const busy = upsert.isPending || updateInterests.isPending || updateVisibility.isPending || setStatus.isPending || updatePhotoOrder.isPending

  return (
    <div className="ob">
      <div className="ob-top">
        <div className="ob-steps">
          {[0, 1, 2].map((i) => (
            <span key={i} className={`ob-pip ${i === step ? 'active' : i < step ? 'done' : ''}`} />
          ))}
        </div>
        <Wordmark size={15} />
      </div>

      {(isEditing || step > 0) && (
        <button
          className="welcome-skip"
          style={{ alignSelf: 'flex-start', marginLeft: -4 }}
          onClick={() => isEditing ? onDone() : setStep(step - 1)}
        >
          ← {isEditing ? 'You' : 'Back'}
        </button>
      )}

      <div className="ob-body">
        {error && <p className="ob-error">{error}</p>}

        {step === 0 && (
          <>
            <h2 className="ob-h">The basics.</h2>
            <p className="ob-p">No age, height, body type or income — ever. Add up to ten photos, then choose your primary.</p>

            <div ref={photoRowRef} className={`photo-row ${draggedPhotoId !== null ? 'dragging' : ''}`}>
              {photoCarousel.map((item) => item.kind === 'photo' ? (
                <div
                  key={item.photo.id}
                  data-photo-id={item.photo.id}
                  data-photo-index={photoList.findIndex((photo) => photo.id === item.photo.id)}
                  className={`pslot draggable ${draggedPhotoId === item.photo.id ? 'dragging' : ''} ${draggedPhotoId !== null && dropIndex === photoList.findIndex((photo) => photo.id === item.photo.id) ? 'drop-target' : ''}`}
                  onPointerDown={(event) => startPhotoDrag(item.photo.id, event)}
                  onPointerMove={movePhotoDrag}
                  onPointerUp={() => void finishPhotoDrag()}
                  onPointerCancel={cancelPhotoDrag}
                  onContextMenu={(event) => event.preventDefault()}
                  aria-grabbed={draggedPhotoId === item.photo.id}
                >
                  <img src={item.photo.thumbUrl || item.photo.displayUrl} alt="" draggable={false} />
                  <button
                    className="report-btn"
                    style={{ opacity: 0.9 }}
                    onPointerDown={(event) => event.stopPropagation()}
                    onClick={() => deletePhoto.mutate(item.photo.id)}
                    aria-label="Remove photo"
                  >
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
                  </button>
                  {item.photo.id === primaryPhotoId ? (
                    <span className="pslot-tag primary">
                      <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2l2.9 6.3 6.9.7-5.1 4.6 1.4 6.8L12 17.8 5.9 20.4l1.4-6.8L2.2 9l6.9-.7z" /></svg>
                      Primary
                    </span>
                  ) : (
                    <button
                      className="pslot-tag make"
                      disabled={setPrimaryPhoto.isPending}
                      onPointerDown={(event) => event.stopPropagation()}
                      onClick={() => makePrimary(item.photo.id)}
                    >
                      Make primary
                    </button>
                  )}
                </div>
              ) : (
                <div key="add-photo" className="pslot">
                  {uploadPhoto.isPending ? 'Uploading…' : `Add (${photoList.length}/${MAX_PROFILE_PHOTOS})`}
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    disabled={uploadPhoto.isPending}
                    onChange={(e) => {
                      void addPhotos(e.target.files)
                      e.target.value = ''
                    }}
                  />
                </div>
              ))}
            </div>

            {photoList.length > 1 && <p className="photo-reorder-hint">Press and drag a photo to change its position.</p>}

            <label className="field">
              Display name
              <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="What should people call you?" />
            </label>

            {!hasExistingProfile && (
              <div className="field">
                <span>How you appear <span className="field-em">(shown as a small, quiet glyph)</span></span>
                <div className="seg-grid">
                  {GENDERS.map((g) => (
                    <button key={g.value} className={`seg ${gender === g.value ? 'on' : ''}`} onClick={() => setGender(g.value)}>
                      {g.label}
                    </button>
                  ))}
                </div>
              </div>
            )}

            <button className="big-btn primary" disabled={busy} onClick={() => void saveBasics()}>
              {upsert.isPending ? 'Saving…' : 'Continue'}
            </button>
          </>
        )}

        {step === 1 && (
          <>
            <h2 className="ob-h">What you're into.</h2>
            <p className="ob-p">Pick up to six. They show up quietly on your card.</p>
            <div className="interest-pick">
              {(interestList.data ?? []).map((interest) => {
                const on = selected.has(interest.id)
                return (
                  <button
                    key={interest.id}
                    className={`pick-chip ${on ? 'on' : ''}`}
                    disabled={!on && selected.size >= 6}
                    onClick={() => toggleInterest(interest.id)}
                  >
                    {interest.label}
                  </button>
                )
              })}
            </div>
            <button className="big-btn primary" disabled={busy} onClick={() => void saveInterests()}>
              {updateInterests.isPending ? 'Saving…' : 'Continue'}
            </button>
          </>
        )}

        {step === 2 && (
          <>
            <h2 className="ob-h">Who can see you.</h2>
            <p className="ob-p">You're in control. Change any of this later in You.</p>
            <div className="toggle-list">
              {gender === 'woman' && (
                <Toggle label="Women appreciating women only" hint="Only women will see and appreciate you." on={vis.womenForWomen} onToggle={() => setVis((v) => ({ ...v, womenForWomen: !v.womenForWomen }))} />
              )}
              <Toggle label="Hide me from people in my own country" hint="Based on your approximate location — never shown." on={vis.hideFromCountry} onToggle={() => setVis((v) => ({ ...v, hideFromCountry: !v.hideFromCountry }))} />
              <Toggle label="Only connect with verified people" on={vis.verifiedOnly} onToggle={() => setVis((v) => ({ ...v, verifiedOnly: !v.verifiedOnly }))} />
            </div>
            <p className="priv-note">Your location is kept coarse — rounded to roughly 11 km, with only broad distance bands. Never your exact spot.</p>
            <button className="big-btn primary" disabled={busy} onClick={() => void activate()}>
              {setStatus.isPending ? 'Activating…' : 'Activate my profile'}
            </button>
          </>
        )}
      </div>
      {draggedPhoto && dragPoint && createPortal(
        <div className="photo-drag-preview" style={{ left: dragPoint.x, top: dragPoint.y }}>
          <img src={draggedPhoto.thumbUrl || draggedPhoto.displayUrl} alt="" />
        </div>,
        document.body,
      )}
    </div>
  )
}

function Toggle({ label, hint, on, onToggle }: { label: string; hint?: string; on: boolean; onToggle: () => void }) {
  return (
    <button type="button" className={`toggle-row ${on ? 'on' : ''}`} onClick={onToggle} aria-pressed={on}>
      <span className="toggle-text">
        <span className="toggle-label">{label}</span>
        {hint && <span className="toggle-hint">{hint}</span>}
      </span>
      <span className={`switch ${on ? 'on' : ''}`}><span className="knob" /></span>
    </button>
  )
}
