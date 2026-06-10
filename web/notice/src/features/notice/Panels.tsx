import type { components } from '../../lib/api/generated/schema'
import { useAppreciationStyle, useReceivedAppreciation } from '../../lib/query/appreciation'
import { useMyFingerprint } from '../../lib/query/socialFingerprint'
import { cat, catInk, catSoft } from './colors'
import { CATEGORY_HUE, CATEGORY_ORDER } from './taxonomy'

type DistItem = components['schemas']['FingerprintDistributionItemResponse']

const pct = (n: number | string) => `${Math.round(Number(n) * 100)}%`

function relativeTime(value: string): string {
  const then = new Date(value).getTime()
  const days = Math.floor((Date.now() - then) / 86_400_000)
  if (days <= 0) return 'Today'
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days} days ago`
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(then)
}

function LockedArt() {
  return (
    <div className="locked-art">
      <span className="locked-ring" />
      <span className="locked-ring r2" />
      <span className="locked-ring r3" />
      <span className="locked-dot" />
    </div>
  )
}

// ── Received ────────────────────────────────────────────────────────────────
export function ReceivedScreen() {
  const received = useReceivedAppreciation(true)

  if (received.isLoading) {
    return <div className="centered-note">Gathering what people have noticed…</div>
  }
  if (received.isError || !received.data) {
    return <div className="centered-note">Your received appreciation could not be loaded.</div>
  }

  const data = received.data
  const total = Number(data.total)
  const traits = data.traits ?? []
  const events = data.events ?? []

  if (total === 0) {
    return (
      <div className="scroll-screen">
        <section className="lead">
          <p className="eyebrow">Received</p>
          <h1>Your profile is live. Now we wait.</h1>
          <p className="lead-sum">
            The moment someone appreciates something about you, it lands here — privately, and only for you.
          </p>
        </section>
        <LockedArt />
        <p className="next-note" style={{ textAlign: 'center', padding: '0 12px' }}>
          Keep appreciating others while you wait — kindness has a way of coming back.
        </p>
        <div className="screen-pad" />
      </div>
    )
  }

  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Received</p>
        <h1>{data.headline}</h1>
        <p className="lead-sum">{data.summary}</p>
        <p className="lead-count">{total} appreciation{total === 1 ? '' : 's'} received privately.</p>
      </section>

      <section className="block">
        <h2 className="block-h">Ways people describe you</h2>
        <div className="recv-grid">
          {traits.map((t) => {
            const hue = Number(t.hue)
            return (
              <article key={t.traitId} className="recv-card">
                <div className="recv-top">
                  <span className="recv-dot" style={{ background: cat(hue) }} />
                  <h3>{t.label}</h3>
                  <span className="recv-count" style={{ background: catSoft(hue), color: catInk(hue) }}>{Number(t.count)}</span>
                </div>
                <p>{t.phrasing}</p>
              </article>
            )
          })}
        </div>
      </section>

      {events.length > 0 && (
        <section className="block">
          <h2 className="block-h">Recent notes</h2>
          <ol className="notes">
            {events.map((e) => {
              const hue = Number(e.hue)
              return (
                <li key={e.id}>
                  <span className="note-dot" style={{ background: cat(hue) }} />
                  <span className="note-text">{e.phrasing}</span>
                  <span className="note-when">{relativeTime(e.createdAt)}</span>
                </li>
              )
            })}
          </ol>
        </section>
      )}
      <div className="screen-pad" />
    </div>
  )
}

// ── Fingerprint radar ───────────────────────────────────────────────────────
export function Radar({ distribution }: { distribution: DistItem[] }) {
  const bySlug = new Map(distribution.map((d) => [d.slug, d]))
  const items = CATEGORY_ORDER.map((slug) => {
    const d = bySlug.get(slug)
    return {
      slug,
      label: d?.label ?? slug,
      share: d ? Number(d.share) : 0,
      hue: CATEGORY_HUE[slug] ?? 38,
    }
  })
  const maxShare = Math.max(...items.map((i) => i.share), 0.01)
  const R = 70
  const pt = (i: number, r: number): [number, number] => {
    const a = -Math.PI / 2 + (i / items.length) * Math.PI * 2
    return [Math.cos(a) * r, Math.sin(a) * r]
  }
  const poly = items.map((it, i) => pt(i, (it.share / maxShare) * R).join(',')).join(' ')

  return (
    <svg viewBox="-152 -116 304 232" className="radar" role="img" aria-label="Perception shape">
      {[0.33, 0.66, 1].map((f) => (
        <polygon key={f} points={items.map((_, i) => pt(i, R * f).join(',')).join(' ')} fill="none" stroke="var(--line-strong)" strokeWidth="1" />
      ))}
      {items.map((_, i) => {
        const [x, y] = pt(i, R)
        return <line key={i} x1="0" y1="0" x2={x} y2={y} stroke="var(--line-strong)" strokeWidth="1" />
      })}
      <polygon points={poly} fill="oklch(0.7 0.13 38 / 0.16)" stroke="var(--coral)" strokeWidth="2.5" strokeLinejoin="round" />
      {items.map((it, i) => {
        const [x, y] = pt(i, (it.share / maxShare) * R)
        const [lx, ly] = pt(i, R + 20)
        return (
          <g key={it.slug}>
            <circle cx={x} cy={y} r="4" fill={cat(it.hue)} />
            <text x={lx} y={ly} textAnchor={Math.abs(lx) < 6 ? 'middle' : lx > 0 ? 'start' : 'end'} dominantBaseline="middle" className="radar-label">{it.label}</text>
          </g>
        )
      })}
    </svg>
  )
}

// ── Fingerprint ─────────────────────────────────────────────────────────────
export function FingerprintScreen() {
  const fingerprint = useMyFingerprint()
  const appreciationStyle = useAppreciationStyle()

  if (fingerprint.isLoading) {
    return <div className="centered-note">Reading the pattern people have noticed…</div>
  }
  if (fingerprint.isError || !fingerprint.data) {
    return <div className="centered-note">Your fingerprint could not be loaded.</div>
  }

  const data = fingerprint.data
  const sampleSize = Number(data.sampleSize)
  const ready = data.status === 'ready'
  const distribution = data.distribution ?? []
  const topTraits = data.topTraits ?? []

  if (!ready) {
    return (
      <div className="scroll-screen">
        <section className="lead">
          <p className="eyebrow">Fingerprint</p>
          <h1>{data.headline}</h1>
          <p className="lead-sum">{data.summary}</p>
        </section>

        <section className="block">
          <h2 className="block-h">Perception shape</h2>
          <div className="radar-card nascent">
            <Radar distribution={distribution} />
            {sampleSize === 0 && <p className="radar-empty">No shape yet</p>}
          </div>
        </section>

        <section className="block">
          <h2 className="block-h">Taking form</h2>
          <div className="form-progress">
            <div className="fp-bar"><span style={{ width: `${Math.min(100, (sampleSize / 20) * 100)}%`, background: 'var(--coral)' }} /></div>
            <p className="next-note"><strong>{sampleSize} of 20.</strong> A handful more and your fingerprint takes its first real shape.</p>
          </div>
        </section>
        <div className="screen-pad" />
      </div>
    )
  }

  const sorted = [...distribution].sort((a, b) => Number(b.share) - Number(a.share))
  const styleData = appreciationStyle.data
  const styleCategories =
    styleData?.status === 'ready'
      ? [...(styleData.categories ?? [])].sort((a, b) => Number(b.share) - Number(a.share))
      : []
  const leadStyle = styleCategories[0]

  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Fingerprint</p>
        <h1>{data.headline}</h1>
        <p className="lead-sum">{data.summary}</p>
        <p className="lead-count">{sampleSize} appreciation moments in this private reading.</p>
      </section>

      <section className="block">
        <h2 className="block-h">Perception shape</h2>
        <div className="radar-card">
          <Radar distribution={distribution} />
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Recurring traits</h2>
        <div className="trait-list">
          {topTraits.map((t) => {
            const hue = Number(t.hue)
            return (
              <article key={t.traitId} className="fp-trait">
                <div className="fp-trait-head">
                  <span className="recv-dot" style={{ background: cat(hue) }} />
                  <h3>{t.label}</h3>
                  <span className="fp-pct" style={{ color: catInk(hue) }}>{pct(t.share)}</span>
                </div>
                <div className="fp-bar"><span style={{ width: pct(t.share), background: cat(hue) }} /></div>
                <p>{t.phrasing}</p>
              </article>
            )
          })}
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Distribution</h2>
        <div className="dist-list">
          {sorted.map((d) => {
            const hue = CATEGORY_HUE[d.slug] ?? 38
            return (
              <div key={d.categoryId} className="dist-row">
                <span className="dist-label">{d.label}</span>
                <div className="fp-bar"><span style={{ width: pct(d.share), background: cat(hue) }} /></div>
                <span className="dist-pct">{pct(d.share)}</span>
              </div>
            )
          })}
        </div>
      </section>

      {leadStyle && (
        <section className="block mirror">
          <div className="mirror-divider"><span /></div>
          <p className="eyebrow mirror-eyebrow">The other side of your fingerprint</p>
          <h2 className="mirror-h">And how you see others</h2>
          <p className="lead-sum mirror-sum">
            The flip side of your fingerprint — a private reading of what you tend to notice first in other people.
          </p>

          <div className="notice-list">
            {styleCategories.map((category) => (
              <div key={category.categoryId} className="dist-row notice-row">
                <span className="dist-label">{category.label}</span>
                <div className="fp-bar"><span style={{ width: pct(category.share) }} /></div>
                <span className="dist-pct">{pct(category.share)}</span>
              </div>
            ))}
          </div>

          <p className="mirror-read">
            You notice <strong>{leadStyle.label.toLowerCase()}</strong> first. {leadStyle.insight}
          </p>
        </section>
      )}
      <div className="screen-pad" />
    </div>
  )
}

// ── Locked (anon hitting Received / Fingerprint) ────────────────────────────
// `incomplete` = a signed-in member who hasn't finished their profile (they deferred
// setup), as opposed to an anonymous browser. The copy and the button then point at
// finishing the profile rather than creating an account.
export function LockedScreen({ kind, incomplete = false, onNudge }: { kind: 'received' | 'fingerprint'; incomplete?: boolean; onNudge: () => void }) {
  const copy =
    kind === 'fingerprint'
      ? {
          eyebrow: 'Fingerprint',
          h: incomplete ? 'Your fingerprint grows once your profile is live.' : 'Your fingerprint grows once people can notice you.',
          p: incomplete
            ? 'Finish your profile and others can start appreciating you. We turn what they choose into a private perception shape — only ever shown to you.'
            : 'Create a profile and others can start appreciating you. We turn what they choose into a private perception shape — only ever shown to you.',
        }
      : {
          eyebrow: 'Received',
          h: incomplete ? "Nothing here yet — your profile isn't live." : "Nothing to receive yet — you're browsing anonymously.",
          p: incomplete
            ? 'Finish your profile to be noticed back. The kind words people choose about you collect here, privately.'
            : 'Set up a profile to be noticed back. The kind words people choose about you collect here, privately.',
        }
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">{copy.eyebrow}</p>
        <h1>{copy.h}</h1>
        <p className="lead-sum">{copy.p}</p>
      </section>
      <LockedArt />
      <button className="big-btn primary" onClick={onNudge}>{incomplete ? 'Finish my profile' : 'Create my profile'}</button>
      <div className="screen-pad" />
    </div>
  )
}
