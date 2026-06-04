// Shared component atoms for the Notice redesign (ADR-025): the wordmark, the
// bottom-nav line icons, and a tinted monogram fallback for profiles without a
// photo. Colour + value helpers live in colors.ts (fast-refresh: components only).
import { hueFromId } from './colors'

export function Wordmark({ size = 17 }: { size?: number }) {
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 7,
        fontFamily: 'var(--display)',
        fontWeight: 700,
        fontSize: size,
        letterSpacing: '-0.01em',
        color: 'var(--ink)',
      }}
    >
      <span
        style={{
          position: 'relative',
          width: size * 0.62,
          height: size * 0.62,
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <span style={{ position: 'absolute', inset: 0, borderRadius: '50%', border: '2px solid var(--coral)' }} />
        <span style={{ width: size * 0.22, height: size * 0.22, borderRadius: '50%', background: 'var(--coral)' }} />
      </span>
      Notice
    </span>
  )
}

export type NavName = 'feed' | 'received' | 'fingerprint' | 'you'

export function NavIcon({ name, active }: { name: NavName; active: boolean }) {
  const s = active ? 'var(--coral-deep)' : 'var(--ink-3)'
  const w = active ? 2.1 : 1.8
  const common = { fill: 'none', stroke: s, strokeWidth: w, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const }
  if (name === 'feed') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <rect x="6" y="3.5" width="12" height="15" rx="3" {...common} />
        <path d="M4 7.5v9a4 4 0 0 0 4 4h8" {...common} opacity={active ? 0.55 : 0.5} />
      </svg>
    )
  }
  if (name === 'received') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <path d="M4 8.5 12 4l8 4.5v7L12 20l-8-4.5z" {...common} />
        <circle cx="12" cy="12" r="2.4" fill={active ? 'var(--coral-deep)' : 'none'} stroke={s} strokeWidth={w} />
      </svg>
    )
  }
  if (name === 'fingerprint') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <circle cx="12" cy="12" r="8.2" {...common} opacity={active ? 0.45 : 0.4} />
        <circle cx="12" cy="12" r="4.6" {...common} />
        <circle cx="12" cy="12" r="1.4" fill={s} stroke="none" />
      </svg>
    )
  }
  return (
    <svg width="25" height="25" viewBox="0 0 24 24">
      <circle cx="12" cy="8.5" r="3.6" {...common} />
      <path d="M5 20a7 7 0 0 1 14 0" {...common} />
    </svg>
  )
}

// Monogram fallback when a profile has no usable photo — tinted by a stable hue.
export function PortraitFallback({ name, seed }: { name: string; seed: string }) {
  const hue = hueFromId(seed)
  const initial = (name?.trim()?.[0] ?? '·').toUpperCase()
  return (
    <div
      style={{
        position: 'relative',
        width: '100%',
        height: '100%',
        overflow: 'hidden',
        background: `linear-gradient(150deg, oklch(0.9 0.06 ${hue}), oklch(0.82 0.09 ${hue}))`,
      }}
    >
      <div
        style={{
          position: 'absolute',
          inset: 0,
          opacity: 0.18,
          backgroundImage: `repeating-linear-gradient(135deg, oklch(0.5 0.1 ${hue}) 0 1.5px, transparent 1.5px 16px)`,
        }}
      />
      <div
        style={{
          position: 'absolute',
          inset: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontFamily: 'var(--display)',
          fontWeight: 700,
          fontSize: 132,
          color: `oklch(0.4 0.12 ${hue})`,
          opacity: 0.5,
        }}
      >
        {initial}
      </div>
    </div>
  )
}
