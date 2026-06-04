import { useState } from 'react'
import type { Profile } from '../../lib/api/profile'
import { useInterests, useUpdateProfileInterests, useUpdateProfileStatus, useUpsertProfile } from '../../lib/query/profile'
import { useDeleteProfilePhoto, useMyPhotos, useUploadProfilePhoto } from '../../lib/query/photos'
import { useUpdateVisibility } from '../../lib/query/settings'
import { Wordmark } from './ui'

const GENDERS: { value: string; label: string }[] = [
  { value: 'woman', label: 'Woman' },
  { value: 'man', label: 'Man' },
  { value: 'non_binary', label: 'Non-binary' },
  { value: 'self_describe', label: 'Rather not say' },
]

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
  const upsert = useUpsertProfile()
  const updateInterests = useUpdateProfileInterests()
  const updateVisibility = useUpdateVisibility()
  const setStatus = useUpdateProfileStatus()
  const interestList = useInterests()
  const photos = useMyPhotos(true)
  const uploadPhoto = useUploadProfilePhoto()
  const deletePhoto = useDeleteProfilePhoto()

  const [step, setStep] = useState(0)
  const [displayName, setDisplayName] = useState(profile?.displayName ?? '')
  const [gender, setGender] = useState(profile?.gender ?? '')
  const [countryCode, setCountryCode] = useState(profile?.countryCode ?? '')
  const [bio, setBio] = useState(profile?.bio ?? '')
  const [selected, setSelected] = useState<Set<string>>(() => new Set(profile?.interests?.map((i) => i.id) ?? []))
  const [vis, setVis] = useState<Visibility>({
    womenForWomen: profile?.visibilityPreferences?.womenForWomen ?? false,
    hideFromCountry: profile?.visibilityPreferences?.hideFromCountry ?? false,
    verifiedOnly: profile?.visibilityPreferences?.verifiedOnly ?? false,
  })
  const [error, setError] = useState<string | null>(null)

  const photoList = photos.data ?? []
  const slots = [0, 1, 2]

  const saveBasics = async () => {
    setError(null)
    if (!displayName.trim() || !gender) {
      setError('Add a display name and choose how you appear.')
      return
    }
    try {
      await upsert.mutateAsync({
        displayName: displayName.trim(),
        gender,
        selfDescribeText: null,
        countryCode: countryCode.trim() || null,
        bio: bio.trim() || null,
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

  const busy = upsert.isPending || updateInterests.isPending || updateVisibility.isPending || setStatus.isPending

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

      {step > 0 && (
        <button className="welcome-skip" style={{ alignSelf: 'flex-start', marginLeft: -4 }} onClick={() => setStep(step - 1)}>
          ← Back
        </button>
      )}

      <div className="ob-body">
        {error && <p className="ob-error">{error}</p>}

        {step === 0 && (
          <>
            <h2 className="ob-h">The basics.</h2>
            <p className="ob-p">No age, height, body type or income — ever. Just enough to be you.</p>

            <div className="photo-row">
              {slots.map((slot) => {
                const photo = photoList[slot]
                return (
                  <div key={slot} className="pslot">
                    {photo ? (
                      <>
                        <img src={photo.thumbUrl || photo.displayUrl} alt="" />
                        <button
                          className="report-btn"
                          style={{ opacity: 0.9 }}
                          onClick={() => deletePhoto.mutate(photo.id)}
                          aria-label="Remove photo"
                        >
                          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
                        </button>
                      </>
                    ) : (
                      <>
                        {uploadPhoto.isPending ? 'Uploading…' : 'Add'}
                        <input
                          type="file"
                          accept="image/*"
                          onChange={(e) => {
                            const file = e.target.files?.[0]
                            if (file) uploadPhoto.mutate(file)
                            e.target.value = ''
                          }}
                        />
                      </>
                    )}
                  </div>
                )
              })}
            </div>

            <label className="field">
              Display name
              <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="What should people call you?" />
            </label>

            <label className="field">
              Country <span className="field-em">(optional, coarse only)</span>
              <input value={countryCode} onChange={(e) => setCountryCode(e.target.value.toUpperCase().slice(0, 2))} placeholder="RO" />
            </label>

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

            <label className="field">
              A line about you <span className="field-em">(optional)</span>
              <input value={bio} onChange={(e) => setBio(e.target.value)} placeholder="Makes a strong espresso…" />
            </label>

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
              <Toggle label="Hide me from people in my own country" on={vis.hideFromCountry} onToggle={() => setVis((v) => ({ ...v, hideFromCountry: !v.hideFromCountry }))} />
              <Toggle label="Only connect with verified people" on={vis.verifiedOnly} onToggle={() => setVis((v) => ({ ...v, verifiedOnly: !v.verifiedOnly }))} />
            </div>
            <p className="priv-note">Your location is kept coarse — rounded to roughly 11 km, with only broad distance bands. Never your exact spot.</p>
            <button className="big-btn primary" disabled={busy} onClick={() => void activate()}>
              {setStatus.isPending ? 'Activating…' : 'Activate my profile'}
            </button>
          </>
        )}
      </div>
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
