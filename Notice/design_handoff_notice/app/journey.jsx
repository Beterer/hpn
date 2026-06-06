// journey.jsx — the first-time moment: incoming toast + fresh/reveal states.
const { useState: useStateJ, useEffect: useEffectJ, useRef: useRefJ } = React;

// count from 0 → target with a brief ease
function useCountUp(target, run, ms = 900) {
  const [n, setN] = useStateJ(run ? 0 : target);
  useEffectJ(() => {
    if (!run) { setN(target); return; }
    let raf; const start = performance.now();
    const tick = (t) => {
      const p = Math.min((t - start) / ms, 1);
      const eased = 1 - Math.pow(1 - p, 3);
      setN(Math.round(eased * target));
      if (p < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, run]);
  return n;
}

// ── Incoming appreciation toast (slides down under the header) ──────────────
function IncomingToast({ data, onOpen, onDismiss }) {
  useEffectJ(() => {
    const id = setTimeout(onDismiss, 8000);
    return () => clearTimeout(id);
  }, []);
  return (
    <button className="toast" onClick={onOpen} style={{ '--h': data.hue }}>
      <span className="toast-burst" style={{ background: cat(data.hue) }} />
      <span className="toast-dot" style={{ background: cat(data.hue) }}>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M8 12.5l3 3 5-6" /></svg>
      </span>
      <span className="toast-text">
        <span className="toast-title">{data.phrasing}</span>
        <span className="toast-sub">Tap to see what they appreciated</span>
      </span>
      <span className="toast-x" onClick={(e) => { e.stopPropagation(); onDismiss(); }} aria-label="Dismiss">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
      </span>
    </button>
  );
}

// ── Received: empty (profile live, nothing yet) ─────────────────────────────
function ReceivedEmpty() {
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Received</p>
        <h1>Your profile is live. Now we wait.</h1>
        <p className="lead-sum">The moment someone appreciates something about you, it lands here — privately, and only for you.</p>
      </section>
      <div className="locked-art">
        <span className="locked-ring" /><span className="locked-ring r2" /><span className="locked-ring r3" />
        <span className="locked-dot" />
      </div>
      <p className="wait-note">Keep appreciating others while you wait — kindness has a way of coming back.</p>
    </div>
  );
}

// ── Received: early (your first appreciation) ───────────────────────────────
function ReceivedEarly({ first, reveal }) {
  const c = CATEGORY_BY_ID[first.category];
  const count = useCountUp(1, reveal, 700);
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow" style={{ color: cat(c.hue) }}>{reveal ? 'Just now' : 'Received'}</p>
        <h1>{reveal ? 'Your first appreciation.' : 'Quietly, it has begun.'}</h1>
        <p className="lead-sum">Someone out there noticed something real about you. This is yours to keep.</p>
      </section>

      <section className="block">
        <div className={`first-card ${reveal ? 'reveal' : ''}`} style={{ '--h': c.hue }}>
          {reveal && <span className="first-glow" style={{ borderColor: cat(c.hue) }} />}
          {reveal && <FirstConfetti hue={c.hue} />}
          <div className="first-top">
            <span className="recv-dot big" style={{ background: cat(c.hue) }} />
            <h3>{first.label}</h3>
            <span className="recv-count big" style={{ background: catSoft(c.hue), color: catInk(c.hue) }}>{count}</span>
          </div>
          <p className="first-phr">{first.phrasing}</p>
          <p className="first-note">{first.note}</p>
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">What happens next</h2>
        <p className="next-note">As more people notice you, the words collect here and your <strong>Fingerprint</strong> begins to take shape — a private picture of how you come across.</p>
      </section>
    </div>
  );
}

// small one-off confetti for the reveal
function FirstConfetti({ hue }) {
  const bits = useRefJ(Array.from({ length: 14 }, (_, i) => {
    const ang = (Math.PI * 2 * i) / 14 + (Math.random() - 0.5) * 0.5;
    const dist = 60 + Math.random() * 80;
    return { x: Math.cos(ang) * dist, y: Math.sin(ang) * dist, rot: (Math.random() - 0.5) * 300, delay: Math.random() * 60, size: 5 + Math.random() * 6, h: hue + (Math.random() - 0.5) * 70, round: Math.random() > 0.5 };
  })).current;
  return (
    <div className="first-confetti">
      {bits.map((b, i) => (
        <span key={i} style={{ '--x': `${b.x}px`, '--y': `${b.y}px`, '--r': `${b.rot}deg`, width: b.size, height: b.size, background: `oklch(0.72 0.15 ${b.h})`, borderRadius: b.round ? '50%' : 2, animationDelay: `${b.delay}ms` }} />
      ))}
    </div>
  );
}

// ── Fingerprint: nascent (forming) ──────────────────────────────────────────
function FingerprintNascent({ seen }) {
  const total = useCountUp(seen ? 1 : 0, seen, 700);
  const dist = seen ? [{ category: 'physical', share: 1 }] : [];
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">Fingerprint</p>
        <h1>{seen ? 'Your shape is just beginning.' : 'Your fingerprint hasn’t formed yet.'}</h1>
        <p className="lead-sum">Your fingerprint is how others tend to perceive you over time — drawn privately from what people appreciate. {seen ? 'One mark is on the map.' : 'It appears once people start noticing you.'}</p>
      </section>

      <section className="block">
        <h2 className="block-h">Perception shape</h2>
        <div className="radar-card nascent">
          <Radar distribution={dist} />
          {!seen && <p className="radar-empty">No shape yet</p>}
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Taking form</h2>
        <div className="form-progress">
          <div className="fp-bar"><span style={{ width: `${(total / 20) * 100}%`, background: 'var(--coral)' }} /></div>
          <p className="next-note"><strong>{total} of 20.</strong> A handful more and your fingerprint takes its first real shape.</p>
        </div>
      </section>
    </div>
  );
}

Object.assign(window, { IncomingToast, ReceivedEmpty, ReceivedEarly, FingerprintNascent, useCountUp });
