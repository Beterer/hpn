import { useEffect, useMemo, useRef, useState } from 'react'
import type { FeedProfile } from '../../lib/api/feed'
import { useAppreciationCategories, useSubmitAppreciation } from '../../lib/query/appreciation'
import { useFeedQueue } from '../../lib/query/feed'
import { cat, catInk, catSoft, genderGlyph } from './colors'
import { PortraitFallback } from './ui'
import { flattenTraits, type FlatTrait } from './taxonomy'

type Phase = 'idle' | 'reacting' | 'flying'

function vibrate(ms: number) {
  try {
    navigator.vibrate?.(ms)
  } catch {
    // not supported — ignore
  }
}

function primaryPhoto(profile: FeedProfile) {
  const photos = profile.photos ?? []
  return photos.find((p) => Number(p.position) === 0) ?? photos[0] ?? null
}

function photoAt(profile: FeedProfile, index: number) {
  const photos = [...(profile.photos ?? [])].sort((a, b) => Number(a.position) - Number(b.position))
  if (photos.length === 0) {
    return null
  }

  return photos[Math.min(Math.max(index, 0), photos.length - 1)]
}

/**
 * The core appreciation loop (ADR-025 redesign): one full-bleed card → appreciate
 * FAB → flattened trait cloud → reward sequence → next. There is no skip/dislike;
 * advancing requires choosing a trait. Honors prefers-reduced-motion via the CSS.
 */
export function FeedScreen() {
  const feed = useFeedQueue()
  const categories = useAppreciationCategories()
  const traits = useMemo(() => flattenTraits(categories.data), [categories.data])

  if (feed.status === 'loading' || categories.isLoading) {
    return <div className="feed-wrap"><p className="centered-note">Finding people to notice…</p></div>
  }

  if (feed.status === 'error') {
    return (
      <div className="feed-wrap">
        <div className="centered-note">
          <p>{feed.error}</p>
          <button className="big-btn ghost" style={{ maxWidth: 200 }} onClick={feed.reload}>Try again</button>
        </div>
      </div>
    )
  }

  if (!feed.current) {
    return (
      <div className="feed-wrap">
        <div className="centered-note">
          <p style={{ fontFamily: 'var(--display)', fontWeight: 700, fontSize: 20, color: 'var(--ink)' }}>
            You're all caught up
          </p>
          <p>There's no one new to notice right now. Check back a little later.</p>
          <button className="big-btn ghost" style={{ maxWidth: 200 }} onClick={feed.reload}>Refresh</button>
        </div>
      </div>
    )
  }

  // Keyed by profile id: each card remounts, so its interaction state resets
  // naturally without a setState-in-effect.
  return (
    <FeedDeck
      key={feed.current.profileId}
      profile={feed.current}
      next={feed.next}
      traits={traits}
      onAdvance={feed.advance}
    />
  )
}

function FeedDeck({
  profile,
  next,
  traits,
  onAdvance,
}: {
  profile: FeedProfile
  next: FeedProfile | null
  traits: FlatTrait[]
  onAdvance: () => void
}) {
  const submit = useSubmitAppreciation()
  const [phase, setPhase] = useState<Phase>('idle')
  const [open, setOpen] = useState(false)
  const [chosen, setChosen] = useState<{ label: string; hue: number } | null>(null)
  const [reported, setReported] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [photoIndex, setPhotoIndex] = useState(0)
  const timers = useRef<number[]>([])
  const photos = [...(profile.photos ?? [])].sort((a, b) => Number(a.position) - Number(b.position))
  const photoCount = photos.length

  useEffect(() => () => timers.current.forEach((t) => clearTimeout(t)), [])

  const pick = (trait: FlatTrait) => {
    if (phase !== 'idle' || submit.isPending || !profile.profileId) {
      return
    }
    vibrate(12)
    setError(null)
    setChosen({ label: trait.label, hue: trait.hue })
    setOpen(false)
    setPhase('reacting')

    submit.mutate(
      {
        request: {
          receiverProfileId: profile.profileId,
          traitId: trait.id,
          photoId: null,
        },
        idempotencyKey: crypto.randomUUID(),
      },
      {
        onSuccess: () => {
          setPhase('flying')
          timers.current.push(window.setTimeout(() => onAdvance(), 460))
        },
        onError: (e) => {
          setPhase('idle')
          setChosen(null)
          setError(e.message)
        },
      },
    )
  }

  const toggleTray = () => {
    if (phase === 'idle') {
      setOpen((o) => !o)
    }
  }

  const shiftPhoto = (delta: number) => {
    if (phase !== 'idle' || photoCount <= 1) {
      return
    }

    setPhotoIndex((current) => (current + delta + photoCount) % photoCount)
  }

  const interests = profile.interests ?? []

  return (
    <div className="feed-wrap" style={{ ['--m' as string]: '14px' }}>
      <div className="feed-stage">
        {next && (
          <article className="card card-behind" aria-hidden="true">
            <div className="card-photo">
              <CardPhoto profile={next} photoIndex={0} />
            </div>
          </article>
        )}

        <article
          className={`card card-front ${phase === 'flying' ? 'is-flying' : ''} ${phase === 'reacting' ? 'is-reacting' : ''}`}
        >
          <div className="card-photo">
            <CardPhoto profile={profile} photoIndex={photoIndex} />

            {photoCount > 1 && (
              <>
                <div className="photo-dots" aria-hidden="true">
                  {photos.map((photo, index) => (
                    <span key={photo.photoId} className={index === photoIndex ? 'active' : ''} />
                  ))}
                </div>
                <button
                  className="photo-nav prev"
                  onClick={() => shiftPhoto(-1)}
                  disabled={phase !== 'idle'}
                  title="Previous photo"
                  aria-label="Previous photo"
                >
                  <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="m15 18-6-6 6-6" /></svg>
                </button>
                <button
                  className="photo-nav next"
                  onClick={() => shiftPhoto(1)}
                  disabled={phase !== 'idle'}
                  title="Next photo"
                  aria-label="Next photo"
                >
                  <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="m9 18 6-6-6-6" /></svg>
                </button>
              </>
            )}

            {/* quiet report — second plane (visual only for now; not wired to the API) */}
            <button
              className={`report-btn ${reported ? 'done' : ''}`}
              onClick={() => setReported(true)}
              title={reported ? 'Reported — thank you' : 'Report this profile'}
              aria-label="Report"
            >
              {reported ? (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
              ) : (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 21V4h11l-1 4 6 0v9H9l1-4H4" /><path d="M4 21V13" /></svg>
              )}
            </button>

            {/* reward layers */}
            {phase === 'reacting' && chosen && (
              <>
                <span className="glow-ring" style={{ borderColor: cat(chosen.hue) }} />
                <Confetti hue={chosen.hue} />
              </>
            )}
            {chosen && phase !== 'idle' && (
              <span className="trait-float" style={{ background: cat(chosen.hue) }}>{chosen.label}</span>
            )}

            <div className="card-scrim" />

            <div className="card-id">
              <div className="card-name-row">
                <h2>{profile.displayName}</h2>
                <span className="gender-glyph" title={profile.gender ?? undefined}>{genderGlyph(profile.gender)}</span>
              </div>
              {interests.length > 0 && (
                <div className="chip-row">
                  {interests.map((it) => <span key={it} className="interest-chip">{it}</span>)}
                </div>
              )}
            </div>

            <button
              className={`fab ${open ? 'open' : ''} ${phase !== 'idle' ? 'busy' : ''}`}
              onClick={toggleTray}
              disabled={phase !== 'idle'}
              aria-label={open ? 'Close' : 'Appreciate'}
            >
              <span className="fab-pulse" />
              {open ? (
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
              ) : (
                <svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3v18M3 12h18" /></svg>
              )}
            </button>

            {open && (
              <div className="tray">
                <p className="cloud-q">Appreciate something real</p>
                <div className="cloud-wrap">
                  {traits.map((t, i) => (
                    <button
                      key={t.id}
                      className="cloud-chip"
                      style={{ background: catSoft(t.hue), color: catInk(t.hue), animationDelay: `${i * 18}ms` }}
                      onClick={() => pick(t)}
                      disabled={submit.isPending}
                    >
                      {t.label}
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </article>
      </div>

      <p className="feed-hint">
        {error ? error : open ? 'Tap a word you mean.' : 'The only way forward is to appreciate.'}
      </p>
    </div>
  )
}

function CardPhoto({ profile, photoIndex }: { profile: FeedProfile; photoIndex: number }) {
  const photo = photoAt(profile, photoIndex) ?? primaryPhoto(profile)
  if (photo?.displayUrl) {
    return <img src={photo.displayUrl} alt={`A photo ${profile.displayName ?? ''} shared`} />
  }
  return <PortraitFallback name={profile.displayName ?? ''} seed={profile.profileId ?? ''} />
}

function Confetti({ hue }: { hue: number }) {
  // Computed once per mount via a lazy state initializer (not a ref read during
  // render), so the burst is stable but the randomness stays out of render.
  const [bits] = useState(() =>
    Array.from({ length: 18 }, (_, i) => {
      const ang = (Math.PI * 2 * i) / 18 + (Math.random() - 0.5) * 0.5
      const dist = 70 + Math.random() * 100
      return {
        x: Math.cos(ang) * dist,
        y: Math.sin(ang) * dist - 30,
        rot: (Math.random() - 0.5) * 320,
        delay: Math.random() * 60,
        size: 6 + Math.random() * 7,
        h: hue + (Math.random() - 0.5) * 70,
        round: Math.random() > 0.5,
      }
    }),
  )
  return (
    <div className="reward-confetti">
      {bits.map((b, i) => (
        <span
          key={i}
          style={{
            ['--x' as string]: `${b.x}px`,
            ['--y' as string]: `${b.y}px`,
            ['--r' as string]: `${b.rot}deg`,
            width: b.size,
            height: b.size,
            background: `oklch(0.72 0.15 ${b.h})`,
            borderRadius: b.round ? '50%' : 3,
            animationDelay: `${b.delay}ms`,
          }}
        />
      ))}
    </div>
  )
}
