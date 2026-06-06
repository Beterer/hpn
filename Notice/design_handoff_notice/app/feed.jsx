// feed.jsx — immersive feed: full-bleed card → appreciate FAB → trait cloud → reward → next.
const { useState: useStateF, useRef: useRefF, useEffect: useEffectF, useCallback } = React;

// density now controls how immersive (margin) + name size, not aspect.
const DENSITY = {
  compact: { margin: 6, name: 35, pad: 20 },
  regular: { margin: 14, name: 31, pad: 20 },
  comfy: { margin: 22, name: 29, pad: 22 },
};

// ── Confetti burst ──────────────────────────────────────────────────────────
function Confetti({ hue }) {
  const bits = useRefF(
    Array.from({ length: 18 }, (_, i) => {
      const ang = (Math.PI * 2 * i) / 18 + (Math.random() - 0.5) * 0.5;
      const dist = 70 + Math.random() * 100;
      return {
        x: Math.cos(ang) * dist, y: Math.sin(ang) * dist - 30,
        rot: (Math.random() - 0.5) * 320, delay: Math.random() * 60,
        size: 6 + Math.random() * 7, h: hue + (Math.random() - 0.5) * 70,
        round: Math.random() > 0.5,
      };
    }),
  ).current;
  return (
    <div className="reward-confetti">
      {bits.map((b, i) => (
        <span key={i} style={{
          '--x': `${b.x}px`, '--y': `${b.y}px`, '--r': `${b.rot}deg`,
          width: b.size, height: b.size, background: `oklch(0.72 0.15 ${b.h})`,
          borderRadius: b.round ? '50%' : 3, animationDelay: `${b.delay}ms`,
        }} />
      ))}
    </div>
  );
}

// ── Trait cloud (shared by tray + sheet) ────────────────────────────────────
function TraitCloud({ onPick }) {
  return (
    <div className="cloud">
      <p className="cloud-q">Appreciate something real</p>
      <div className="cloud-wrap">
        {ALL_TRAITS.map((t, i) => (
          <button key={t.label} className="cloud-chip"
            style={{ '--h': t.hue, background: catSoft(t.hue), color: catInk(t.hue), animationDelay: `${i * 18}ms` }}
            onClick={() => onPick(t)}>
            {t.label}
          </button>
        ))}
      </div>
    </div>
  );
}

// ── Feed screen ─────────────────────────────────────────────────────────────
function FeedScreen({ tweaks }) {
  const [index, setIndex] = useStateF(0);
  const [phase, setPhase] = useStateF('idle'); // idle | reacting | flying
  const [open, setOpen] = useStateF(false);
  const [chosen, setChosen] = useStateF(null);
  const [cardKey, setCardKey] = useStateF(0);
  const [reported, setReported] = useStateF(false);
  const timers = useRefF([]);

  const d = DENSITY[tweaks.density] || DENSITY.regular;
  const radius = tweaks.cardRadius;
  const rewardStyle = tweaks.reward;
  const trayMode = tweaks.flow; // tray | sheet

  const profile = PROFILES[index % PROFILES.length];
  const next = PROFILES[(index + 1) % PROFILES.length];

  useEffectF(() => () => timers.current.forEach(clearTimeout), []);

  const pick = useCallback((t) => {
    if (phase !== 'idle') return;
    try { navigator.vibrate && navigator.vibrate(12); } catch (e) {}
    setChosen({ trait: t.label, hue: t.hue });
    setOpen(false);
    setPhase('reacting');
    timers.current.push(setTimeout(() => setPhase('flying'), 560));
    timers.current.push(setTimeout(() => {
      setIndex((i) => (i + 1) % PROFILES.length);
      setChosen(null); setReported(false); setPhase('idle'); setCardKey((k) => k + 1);
    }, 1020));
  }, [phase]);

  const toggle = () => { if (phase === 'idle') setOpen((o) => !o); };

  return (
    <div className="feed-wrap" style={{ '--m': `${d.margin}px` }}>
      <div className="feed-stage">
        {/* card behind */}
        <article className="card card-behind" style={{ borderRadius: radius }} aria-hidden="true">
          <div className="card-photo" style={{ borderRadius: radius }}><Portrait profile={next} /></div>
        </article>

        {/* active card */}
        <article key={cardKey}
          className={`card card-front ${phase === 'flying' ? 'is-flying' : ''} ${phase === 'reacting' ? 'is-reacting' : ''}`}
          style={{ borderRadius: radius, '--h': chosen ? chosen.hue : 38 }}>
          <div className="card-photo" style={{ borderRadius: radius }}>
            <Portrait profile={profile} />

            {/* quiet report — second plane, top-right */}
            <button className={`report-btn ${reported ? 'done' : ''}`} onClick={() => setReported(true)}
              title={reported ? 'Reported — thank you' : 'Report this profile'} aria-label="Report">
              {reported ? (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
              ) : (
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 21V4h11l-1 4 6 0v9H9l1-4H4" /><path d="M4 21V13" /></svg>
              )}
            </button>

            {/* reward layers */}
            {phase === 'reacting' && (rewardStyle === 'glow' || rewardStyle === 'both') && (
              <span className="glow-ring" style={{ borderColor: cat(chosen.hue) }} />
            )}
            {phase === 'reacting' && (rewardStyle === 'confetti' || rewardStyle === 'both') && (
              <Confetti hue={chosen.hue} />
            )}
            {chosen && phase !== 'idle' && (
              <span className="trait-float" style={{ background: cat(chosen.hue) }}>{chosen.trait}</span>
            )}

            <div className="card-scrim" />

            {/* identity — name, glyph, interests only */}
            <div className="card-id" style={{ padding: d.pad, paddingRight: 78 }}>
              <div className="card-name-row">
                <h2 style={{ fontSize: d.name }}>{profile.name}</h2>
                <span className="gender-glyph" title={profile.gender}>{GENDER_GLYPH[profile.gender]}</span>
              </div>
              <div className="chip-row">
                {profile.interests.map((it) => <span key={it} className="interest-chip">{it}</span>)}
              </div>
            </div>

            {/* the single action — appreciate FAB, pulsing invite */}
            <button className={`fab ${open ? 'open' : ''} ${phase !== 'idle' ? 'busy' : ''}`}
              onClick={toggle} disabled={phase !== 'idle'} aria-label={open ? 'Close' : 'Appreciate'}>
              <span className="fab-pulse" />
              {open ? (
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
              ) : (
                <svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3v18M3 12h18" /></svg>
              )}
            </button>

            {/* tray reaction — anchored to the FAB */}
            {trayMode === 'tray' && open && (
              <div className="tray">
                <TraitCloud onPick={pick} />
              </div>
            )}
          </div>
        </article>
      </div>

      {/* first-tap hint, lives under the card */}
      <p className="feed-hint">{open ? 'Tap a word you mean.' : 'The only way forward is to appreciate.'}</p>

      {/* sheet reaction flow */}
      {trayMode === 'sheet' && (
        <React.Fragment>
          <div className={`sheet-scrim ${open ? 'open' : ''}`} onClick={() => setOpen(false)} />
          <div className={`sheet ${open ? 'open' : ''}`}>
            <div className="sheet-grip" />
            <TraitCloud onPick={pick} />
          </div>
        </React.Fragment>
      )}
    </div>
  );
}

Object.assign(window, { FeedScreen });
