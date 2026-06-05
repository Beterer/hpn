import { useEffect, useMemo, useRef, useState } from 'react'
import type { FeedProfile } from '../../lib/api/feed'
import { useAppreciationCategories, useSubmitAppreciation } from '../../lib/query/appreciation'
import { useFeedQueue } from '../../lib/query/feed'
import { useSubmitReport } from '../../lib/query/reports'
import { useBlockProfile } from '../../lib/query/settings'
import { ApiError } from '../../lib/api/appreciation'
import { cat, catInk, catSoft, genderGlyph } from './colors'
import { PortraitFallback } from './ui'
import { flattenTraits, type FlatTrait } from './taxonomy'

type Phase = 'idle' | 'reacting' | 'flying'

// The reward beat (matches the design mock): the glow + confetti hold for
// REACT_MS so they actually land as a dopamine hit, *then* the card slides away
// over FLY_MS. These are fixed timings, deliberately decoupled from the network
// — otherwise an instant localhost response collapses the whole reward into a
// blink. The CSS keyframes (glow .6s, confetti .9s, float 1s, flyAway .46s) are
// tuned to this envelope.
const REACT_MS = 560
const FLY_MS = 460

// The three reasons surfaced on the card's quiet report tray. We deliberately
// keep it to a short, dignified set (the API in reports.ts accepts the full
// moderation.report_type taxonomy) — one for content, one for authenticity,
// one for the most safety-critical case. Labels are the human phrasing; the
// value is the snake_case slug the backend expects.
const REPORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'inappropriate_content', label: 'Inappropriate photo' },
  { value: 'ai_generated', label: 'Looks AI-generated' },
  { value: 'underage', label: 'Seems underage' },
]

// Appreciation failures where retrying *this* card can never succeed — the
// receiver became unappreciable (paused / blocked / gone between fetch and tap),
// or this category was already appreciated. Because the feed has no skip, we drop
// the card instead of wedging the user on it. Everything else (network, a
// retired trait, validation) keeps the card and surfaces a retryable error.
const PERMANENT_APPRECIATION_PROBLEMS = new Set([
  'profile-unavailable',
  'duplicate-appreciation',
  'self-appreciation',
])

function isPermanentAppreciationFailure(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    error.problem !== null &&
    PERMANENT_APPRECIATION_PROBLEMS.has(error.problem)
  )
}

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
  const report = useSubmitReport()
  const block = useBlockProfile()
  const [phase, setPhase] = useState<Phase>('idle')
  const [open, setOpen] = useState(false)
  const [reportOpen, setReportOpen] = useState(false)
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

    // The card flies only once the reward has had its full visual beat (REACT_MS)
    // *and* the appreciation is confirmed saved — whichever lands last. Running
    // the beat on a fixed timer (rather than the network response) is what makes
    // the glow + confetti read as a reward instead of an instant blink, while
    // still never advancing past a failed submit.
    let beatDone = false
    let saved = false
    const flyIfReady = () => {
      if (!beatDone || !saved) {
        return
      }
      setPhase('flying')
      timers.current.push(window.setTimeout(() => onAdvance(), FLY_MS))
    }

    timers.current.push(
      window.setTimeout(() => {
        beatDone = true
        flyIfReady()
      }, REACT_MS),
    )

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
          saved = true
          flyIfReady()
        },
        onError: (e) => {
          // Can't ever appreciate this card → skip it rather than trap the user
          // (the feed offers no other way forward). Report/Block are still there
          // for a deliberate exit; this just unwedges the common races.
          if (isPermanentAppreciationFailure(e)) {
            onAdvance()
            return
          }
          setPhase('idle')
          setChosen(null)
          setError(e.message)
        },
      },
    )
  }

  const toggleTray = () => {
    if (phase === 'idle') {
      setReportOpen(false)
      setOpen((o) => !o)
    }
  }

  const toggleReport = () => {
    if (phase !== 'idle' || reported) {
      return
    }
    setError(null)
    setOpen(false)
    setReportOpen((o) => !o)
  }

  // Mirrors pick(): one tap on a reason fires the report and acknowledges it in
  // place. We keep the card where it is — reporting isn't appreciating, so it
  // must not advance the feed — and flip the button to its "received" state.
  const sendReport = (type: string) => {
    if (report.isPending || !profile.profileId) {
      return
    }
    vibrate(10)
    setError(null)
    report.mutate(
      { targetProfileId: profile.profileId, type },
      {
        onSuccess: () => {
          setReported(true)
          setReportOpen(false)
        },
        onError: (e) => setError(e.message),
      },
    )
  }

  // Block is the decisive exit, not a report reason: one tap drops the card for
  // good (the queue's seen-set + the eligibility filter keep them gone both
  // directions). It's reversible from You → Blocked, so no confirm step. On
  // success the card advances and this component unmounts, so state resets.
  const blockAndAdvance = () => {
    if (block.isPending || phase !== 'idle' || !profile.profileId) {
      return
    }
    vibrate(14)
    setError(null)
    block.mutate(profile.profileId, {
      onSuccess: () => onAdvance(),
      onError: (e) => setError(e.message),
    })
  }

  const shiftPhoto = (delta: number) => {
    if (phase !== 'idle' || photoCount <= 1) {
      return
    }

    setPhotoIndex((current) => (current + delta + photoCount) % photoCount)
  }

  // Mobile gets swipe instead of arrows (the arrows are hidden on coarse
  // pointers via CSS). A tap won't pass the distance threshold, so the FAB /
  // report / dots underneath keep working; vertical drags are ignored so the
  // page can still scroll.
  const touchStart = useRef<{ x: number; y: number } | null>(null)
  const onTouchStart = (e: React.TouchEvent) => {
    if (phase !== 'idle' || open || photoCount <= 1) {
      touchStart.current = null
      return
    }
    const t = e.touches[0]
    touchStart.current = { x: t.clientX, y: t.clientY }
  }
  const onTouchEnd = (e: React.TouchEvent) => {
    const start = touchStart.current
    touchStart.current = null
    if (!start) {
      return
    }
    const t = e.changedTouches[0]
    const dx = t.clientX - start.x
    const dy = t.clientY - start.y
    if (Math.abs(dx) < 44 || Math.abs(dx) <= Math.abs(dy)) {
      return
    }
    shiftPhoto(dx < 0 ? 1 : -1) // swipe left → next, right → previous
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
          <div className="card-photo" onTouchStart={onTouchStart} onTouchEnd={onTouchEnd}>
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

            {/* quiet report — second plane */}
            <button
              className={`report-btn ${reported ? 'done' : ''} ${reportOpen ? 'open' : ''}`}
              onClick={toggleReport}
              disabled={phase !== 'idle' || reported}
              title={reported ? 'Reported — thank you' : 'Report this profile'}
              aria-label="Report"
              aria-expanded={reportOpen}
            >
              {reported ? (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
              ) : (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 21V4h11l-1 4 6 0v9H9l1-4H4" /><path d="M4 21V13" /></svg>
              )}
            </button>

            {reportOpen && (
              <div className="tray report-tray">
                <p className="cloud-q">Something off here?</p>
                <div className="cloud-wrap">
                  {REPORT_OPTIONS.map((o) => (
                    <button
                      key={o.value}
                      className="cloud-chip report-chip"
                      onClick={() => sendReport(o.value)}
                      disabled={report.isPending || block.isPending}
                    >
                      {o.label}
                    </button>
                  ))}
                </div>
                <p className="report-foot">Private. We review every report.</p>
                <button
                  className="block-row"
                  onClick={blockAndAdvance}
                  disabled={report.isPending || block.isPending}
                >
                  <span className="block-row-main">{block.isPending ? 'Blocking…' : 'Block this person'}</span>
                  <span className="block-row-sub">You won't see each other again.</span>
                </button>
              </div>
            )}

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
        {error
          ? error
          : reportOpen
            ? 'Tap a reason — it stays private.'
            : reported
              ? 'Reported — thank you for looking out.'
              : open
                ? 'Tap a word you mean.'
                : 'The only way forward is to appreciate.'}
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
