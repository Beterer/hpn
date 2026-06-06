// panels.jsx — Received, Fingerprint, Onboarding, You, and nav chrome.
const { useState: useStateP } = React;

// ── Header (in-app top bar) ─────────────────────────────────────────────────
function AppHeader({ anon, onNudge, onGear }) {
  return (
    <header className="app-header">
      <Wordmark />
      {anon ? (
        <button className="nudge" onClick={onNudge} aria-label="Create a profile to be noticed back">
          <span className="nudge-halo" />
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="9" cy="8" r="3.4" /><path d="M3.5 20a5.5 5.5 0 0 1 11 0" /><path d="M18 7v6M21 10h-6" /></svg>
          <span className="nudge-label">Be noticed back</span>
        </button>
      ) : (
        <button className="ghost-btn" onClick={onGear} aria-label="Settings">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="3" />
          <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-2.9 1.2V21a2 2 0 1 1-4 0v-.1A1.7 1.7 0 0 0 7 19.4a1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0-1.2-2.9H1a2 2 0 1 1 0-4h.1A1.7 1.7 0 0 0 2.6 7a1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3H7a1.7 1.7 0 0 0 1-1.5V1a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9V7a1.7 1.7 0 0 0 1.5 1H23a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z" />
        </svg>
      </button>
      )}
    </header>
  );
}

// ── Locked state (anon hitting Received / Fingerprint) ──────────────────────
function LockedScreen({ kind, onNudge }) {
  const copy = kind === 'fingerprint'
    ? { eyebrow: 'Fingerprint', h: 'Your fingerprint grows once people can notice you.', p: 'Create a profile and others can start appreciating you. We turn what they choose into a private perception shape — only ever shown to you.' }
    : { eyebrow: 'Received', h: 'Nothing to receive yet — you’re browsing anonymously.', p: 'Set up a profile to be noticed back. The kind words people choose about you collect here, privately.' };
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">{copy.eyebrow}</p>
        <h1>{copy.h}</h1>
        <p className="lead-sum">{copy.p}</p>
      </section>
      <div className="locked-art">
        <span className="locked-ring" /><span className="locked-ring r2" /><span className="locked-ring r3" />
        <span className="locked-dot" />
      </div>
      <button className="big-btn primary" style={{ margin: '8px 0 0' }} onClick={onNudge}>Create my profile</button>
    </div>
  );
}

// ── Bottom nav ──────────────────────────────────────────────────────────────
function BottomNav({ tab, setTab }) {
  const tabs = [
    { id: 'feed', label: 'Notice' },
    { id: 'received', label: 'Received' },
    { id: 'fingerprint', label: 'Fingerprint' },
    { id: 'you', label: 'You' },
  ];
  return (
    <nav className="bottom-nav">
      {tabs.map((t) => (
        <button key={t.id} className={`nav-item ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)}>
          <Icon name={t.id} active={tab === t.id} />
          <span>{t.label}</span>
        </button>
      ))}
    </nav>
  );
}

// ── Received ────────────────────────────────────────────────────────────────
function ReceivedScreen() {
  const r = RECEIVED;
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Received</p>
        <h1>{r.headline}</h1>
        <p className="lead-sum">{r.summary}</p>
        <p className="lead-count">{r.total} appreciations received privately.</p>
      </section>

      <section className="block">
        <h2 className="block-h">Ways people describe you</h2>
        <div className="recv-grid">
          {r.traits.map((t) => {
            const c = CATEGORY_BY_ID[t.category];
            return (
              <article key={t.label} className="recv-card" style={{ '--h': c.hue }}>
                <div className="recv-top">
                  <span className="recv-dot" style={{ background: cat(c.hue) }} />
                  <h3>{t.label}</h3>
                  <span className="recv-count" style={{ background: catSoft(c.hue), color: catInk(c.hue) }}>{t.count}</span>
                </div>
                <p>{t.phrasing}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Recent notes</h2>
        <ol className="notes">
          {r.recent.map((e) => {
            const c = CATEGORY_BY_ID[e.category];
            return (
              <li key={e.id}>
                <span className="note-dot" style={{ background: cat(c.hue) }} />
                <span className="note-text">{e.phrasing}</span>
                <span className="note-when">{e.when}</span>
              </li>
            );
          })}
        </ol>
      </section>
      <div className="screen-pad" />
    </div>
  );
}

// ── Fingerprint radar ───────────────────────────────────────────────────────
function Radar({ distribution }) {
  const items = CATEGORIES.map((c) => {
    const d = distribution.find((x) => x.category === c.id);
    return { ...c, share: d ? d.share : 0 };
  });
  const maxShare = Math.max(...items.map((i) => i.share), 0.01);
  const R = 70;
  const pt = (i, r) => {
    const a = -Math.PI / 2 + (i / items.length) * Math.PI * 2;
    return [Math.cos(a) * r, Math.sin(a) * r];
  };
  const poly = items.map((it, i) => pt(i, (it.share / maxShare) * R).join(',')).join(' ');
  return (
    <svg viewBox="-152 -116 304 232" className="radar">
      {[0.33, 0.66, 1].map((f) => (
        <polygon key={f} points={items.map((_, i) => pt(i, R * f).join(',')).join(' ')} fill="none" stroke="var(--line-strong)" strokeWidth="1" />
      ))}
      {items.map((_, i) => { const [x, y] = pt(i, R); return <line key={i} x1="0" y1="0" x2={x} y2={y} stroke="var(--line-strong)" strokeWidth="1" />; })}
      <polygon points={poly} fill="oklch(0.7 0.13 38 / 0.16)" stroke="var(--coral)" strokeWidth="2.5" strokeLinejoin="round" />
      {items.map((it, i) => {
        const [x, y] = pt(i, (it.share / maxShare) * R);
        const [lx, ly] = pt(i, R + 20);
        return (
          <g key={it.id}>
            <circle cx={x} cy={y} r="4" fill={cat(it.hue)} />
            <text x={lx} y={ly} textAnchor={Math.abs(lx) < 6 ? 'middle' : lx > 0 ? 'start' : 'end'} dominantBaseline="middle" className="radar-label">{it.label}</text>
          </g>
        );
      })}
    </svg>
  );
}

function FingerprintScreen() {
  const f = FINGERPRINT;
  const pct = (n) => `${Math.round(n * 100)}%`;
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Fingerprint</p>
        <h1>{f.headline}</h1>
        <p className="lead-sum">{f.summary}</p>
        <p className="lead-count">{f.total} appreciation moments in this private reading.</p>
      </section>

      <section className="block">
        <h2 className="block-h">Perception shape</h2>
        <div className="radar-card">
          <Radar distribution={f.distribution} />
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Recurring traits</h2>
        <div className="trait-list">
          {f.topTraits.map((t) => {
            const c = CATEGORY_BY_ID[t.category];
            return (
              <article key={t.label} className="fp-trait" style={{ '--h': c.hue }}>
                <div className="fp-trait-head">
                  <span className="recv-dot" style={{ background: cat(c.hue) }} />
                  <h3>{t.label}</h3>
                  <span className="fp-pct" style={{ color: catInk(c.hue) }}>{pct(t.share)}</span>
                </div>
                <div className="fp-bar"><span style={{ width: pct(t.share), background: cat(c.hue) }} /></div>
                <p>{t.phrasing}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Distribution</h2>
        <div className="dist-list">
          {[...f.distribution].sort((a, b) => b.share - a.share).map((d) => {
            const c = CATEGORY_BY_ID[d.category];
            return (
              <div key={d.category} className="dist-row">
                <span className="dist-label">{c.label}</span>
                <div className="fp-bar"><span style={{ width: pct(d.share), background: cat(c.hue) }} /></div>
                <span className="dist-pct">{pct(d.share)}</span>
              </div>
            );
          })}
        </div>
      </section>
      <div className="screen-pad" />
    </div>
  );
}

Object.assign(window, { AppHeader, LockedScreen, BottomNav, ReceivedScreen, Radar, FingerprintScreen });
