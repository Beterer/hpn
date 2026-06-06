// ui.jsx — shared atoms: wordmark, nav icons, small primitives.
const { useState, useEffect, useRef } = React;

// Notice wordmark — a small filled dot (an eye / a noticing mark) + word.
function Wordmark({ size = 17 }) {
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 7, fontFamily: 'var(--display)', fontWeight: 700, fontSize: size, letterSpacing: '-0.01em', color: 'var(--ink)' }}>
      <span style={{ position: 'relative', width: size * 0.62, height: size * 0.62, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}>
        <span style={{ position: 'absolute', inset: 0, borderRadius: '50%', border: '2px solid var(--coral)' }} />
        <span style={{ width: size * 0.22, height: size * 0.22, borderRadius: '50%', background: 'var(--coral)' }} />
      </span>
      Notice
    </span>
  );
}

// ── Bottom-nav line icons (simple geometric strokes) ────────────────────────
function Icon({ name, active }) {
  const s = active ? 'var(--coral-deep)' : 'var(--ink-3)';
  const w = active ? 2.1 : 1.8;
  const common = { fill: 'none', stroke: s, strokeWidth: w, strokeLinecap: 'round', strokeLinejoin: 'round' };
  if (name === 'feed') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <rect x="6" y="3.5" width="12" height="15" rx="3" {...common} />
        <path d="M4 7.5v9a4 4 0 0 0 4 4h8" {...common} opacity={active ? 0.55 : 0.5} />
      </svg>
    );
  }
  if (name === 'received') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <path d="M4 8.5 12 4l8 4.5v7L12 20l-8-4.5z" {...common} />
        <circle cx="12" cy="12" r="2.4" fill={active ? 'var(--coral-deep)' : 'none'} stroke={s} strokeWidth={w} />
      </svg>
    );
  }
  if (name === 'fingerprint') {
    return (
      <svg width="25" height="25" viewBox="0 0 24 24">
        <circle cx="12" cy="12" r="8.2" {...common} opacity={active ? 0.45 : 0.4} />
        <circle cx="12" cy="12" r="4.6" {...common} />
        <circle cx="12" cy="12" r="1.4" fill={s} stroke="none" />
      </svg>
    );
  }
  // you
  return (
    <svg width="25" height="25" viewBox="0 0 24 24">
      <circle cx="12" cy="8.5" r="3.6" {...common} />
      <path d="M5 20a7 7 0 0 1 14 0" {...common} />
    </svg>
  );
}

// Tiny back chevron
function Back({ onClick, label = 'Back' }) {
  return (
    <button className="ghost-btn" onClick={onClick} aria-label={label}>
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M15 5l-7 7 7 7" />
      </svg>
    </button>
  );
}

// A monogram "portrait" placeholder panel, tinted per profile.
function Portrait({ profile, style = {} }) {
  const hue = profile.tone;
  return (
    <div style={{
      position: 'relative', width: '100%', height: '100%', overflow: 'hidden',
      background: `linear-gradient(150deg, oklch(0.9 0.06 ${hue}), oklch(0.82 0.09 ${hue}))`,
      ...style,
    }}>
      {/* soft diagonal stripes so it reads as a placeholder, not a flat block */}
      <div style={{
        position: 'absolute', inset: 0, opacity: 0.18,
        backgroundImage: `repeating-linear-gradient(135deg, oklch(0.5 0.1 ${hue}) 0 1.5px, transparent 1.5px 16px)`,
      }} />
      <div style={{
        position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center',
        fontFamily: 'var(--display)', fontWeight: 700, fontSize: 132, color: `oklch(0.4 0.12 ${hue})`, opacity: 0.5,
      }}>{profile.mono}</div>
      <span style={{
        position: 'absolute', left: 14, top: 14, fontFamily: 'var(--mono)', fontSize: 10.5,
        letterSpacing: '0.08em', textTransform: 'uppercase', color: `oklch(0.35 0.08 ${hue})`,
        background: 'rgba(255,255,255,0.55)', padding: '3px 7px', borderRadius: 6, backdropFilter: 'blur(2px)',
      }}>portrait</span>
    </div>
  );
}

Object.assign(window, { Wordmark, Icon, Back, Portrait });
