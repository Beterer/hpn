// onboarding.jsx — entry flow (two paths + privacy) and the You screen.
const { useState: useStateO } = React;

function Toggle({ label, hint, checked, onChange }) {
  return (
    <button className={`toggle-row ${checked ? 'on' : ''}`} onClick={() => onChange(!checked)}>
      <span className="toggle-text">
        <span className="toggle-label">{label}</span>
        {hint && <span className="toggle-hint">{hint}</span>}
      </span>
      <span className={`switch ${checked ? 'on' : ''}`}><span className="knob" /></span>
    </button>
  );
}

const GENDERS = [
  { id: 'woman', label: 'Woman' },
  { id: 'man', label: 'Man' },
  { id: 'nonbinary', label: 'Non-binary' },
  { id: 'undisclosed', label: 'Rather not say' },
];

// ── Onboarding / Auth (overlay; doubles as sign-in) ─────────────────────────
function OnboardingFlow({ onEnter, onClose }) {
  const [step, setStep] = useStateO('auth'); // auth | signin | basics | interests | privacy
  const [name, setName] = useStateO('');
  const [email, setEmail] = useStateO('');
  const [gender, setGender] = useStateO('woman');
  const [picked, setPicked] = useStateO([]);
  const [priv, setPriv] = useStateO({ womenOnly: false, hideCountry: false, outsideOnly: false, verifiedOnly: false });

  const toggleInterest = (i) =>
    setPicked((p) => (p.includes(i) ? p.filter((x) => x !== i) : p.length < 6 ? [...p, i] : p));

  // Entry: create profile OR sign in — same screen.
  if (step === 'auth' || step === 'signin') {
    const signin = step === 'signin';
    return (
      <div className="ob welcome">
        <div className="ob-top">
          <button className="ghost-btn" onClick={onClose} aria-label="Keep browsing">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 6l12 12M18 6 6 18" /></svg>
          </button>
          <Wordmark size={18} />
          <span style={{ width: 38 }} />
        </div>
        <div className="welcome-mid">
          <h1 className="welcome-h">{signin ? <React.Fragment>Welcome<br />back.</React.Fragment> : <React.Fragment>Be <em>noticed</em><br />back.</React.Fragment>}</h1>
          <p className="welcome-sub">{signin
            ? 'Sign in to pick up your fingerprint and the words people have chosen about you.'
            : 'You can keep appreciating others anonymously. Create a profile to receive appreciation too — and grow a fingerprint of your own.'}</p>
          <label className="field" style={{ marginTop: 22 }}>
            <span>Email</span>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@email.com" />
          </label>
        </div>
        <div className="welcome-actions">
          {signin ? (
            <React.Fragment>
              <button className="big-btn primary" onClick={() => onEnter('member', 'established')}>Sign in</button>
              <button className="big-btn ghost" onClick={() => setStep('auth')}>New here? Create a profile</button>
            </React.Fragment>
          ) : (
            <React.Fragment>
              <button className="big-btn primary" onClick={() => setStep('basics')}>Create my profile</button>
              <button className="big-btn ghost" onClick={() => setStep('signin')}>I already have an account</button>
            </React.Fragment>
          )}
          <button className="welcome-skip" onClick={onClose}>Keep browsing anonymously</button>
        </div>
      </div>
    );
  }

  const Stepper = () => (
    <div className="ob-steps">
      {['basics', 'interests', 'privacy'].map((s, i) => (
        <span key={s} className={`ob-pip ${step === s ? 'active' : ''} ${['basics', 'interests', 'privacy'].indexOf(step) > i ? 'done' : ''}`} />
      ))}
    </div>
  );

  return (
    <div className="ob">
      <div className="ob-top">
        <Back onClick={() => setStep(step === 'basics' ? 'auth' : step === 'interests' ? 'basics' : 'interests')} />
        <Stepper />
        <span style={{ width: 34 }} />
      </div>

      <div className="ob-body">
        {step === 'basics' && (
          <React.Fragment>
            <h2 className="ob-h">Set up how you want to be noticed.</h2>
            <p className="ob-p">No age, height, body type or income — ever. Just a few photos and a name.</p>
            <div className="photo-row">
              <image-slot id="ob-photo-1" className="pslot" shape="rounded" radius="18" placeholder="Add photo"></image-slot>
              <image-slot id="ob-photo-2" className="pslot" shape="rounded" radius="18" placeholder="+"></image-slot>
              <image-slot id="ob-photo-3" className="pslot" shape="rounded" radius="18" placeholder="+"></image-slot>
            </div>
            <label className="field">
              <span>Display name</span>
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder="What should people call you?" />
            </label>
            <div className="field">
              <span>Gender <em className="field-em">— shown as a small, quiet glyph</em></span>
              <div className="seg-grid">
                {GENDERS.map((g) => (
                  <button key={g.id} className={`seg ${gender === g.id ? 'on' : ''}`} onClick={() => setGender(g.id)}>{g.label}</button>
                ))}
              </div>
            </div>
            <button className="big-btn primary" onClick={() => setStep('interests')}>Continue</button>
          </React.Fragment>
        )}

        {step === 'interests' && (
          <React.Fragment>
            <h2 className="ob-h">A few ordinary things you love.</h2>
            <p className="ob-p">Pick up to six. They give people a gentle place to start noticing.</p>
            <div className="interest-pick">
              {INTERESTS.map((i) => (
                <button key={i} className={`pick-chip ${picked.includes(i) ? 'on' : ''}`} onClick={() => toggleInterest(i)}>{i}</button>
              ))}
            </div>
            <button className="big-btn primary" onClick={() => setStep('privacy')}>Continue</button>
          </React.Fragment>
        )}

        {step === 'privacy' && (
          <React.Fragment>
            <h2 className="ob-h">You decide who can see you.</h2>
            <p className="ob-p">Plain controls, on from the start. You can change any of these later.</p>
            <div className="toggle-list">
              {gender === 'woman' && (
                <Toggle label="Women appreciating women only" hint="Only women will see your profile." checked={priv.womenOnly} onChange={(v) => setPriv((p) => ({ ...p, womenOnly: v }))} />
              )}
              <Toggle label="Hide me from people in my own country" hint="People in your country won't see you." checked={priv.hideCountry} onChange={(v) => setPriv((p) => ({ ...p, hideCountry: v }))} />
              <Toggle label="Only show me people outside my country" hint="Your feed skips your own country." checked={priv.outsideOnly} onChange={(v) => setPriv((p) => ({ ...p, outsideOnly: v }))} />
              <Toggle label="Only connect with verified people" hint="Limit to profiles that completed verification." checked={priv.verifiedOnly} onChange={(v) => setPriv((p) => ({ ...p, verifiedOnly: v }))} />
            </div>
            <p className="priv-note">Location stays coarse — rounded to roughly 11 km, shown only as broad distance bands. Never your exact spot.</p>
            <button className="big-btn primary" onClick={() => onEnter('member', 'fresh')}>Activate my profile</button>
          </React.Fragment>
        )}
      </div>
    </div>
  );
}

// ── You (profile + settings) ────────────────────────────────────────────────
function YouScreen({ mode, onRestart, onSignOut }) {
  const [priv, setPriv] = useStateO({ womenOnly: false, hideCountry: false, outsideOnly: false, verifiedOnly: false, paused: false });
  const guest = mode === 'guest';
  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">You</p>
        {guest ? (
          <React.Fragment>
            <h1>You're appreciating anonymously.</h1>
            <p className="lead-sum">No one can notice you back yet. Create a profile to receive appreciation — and grow a fingerprint of your own.</p>
            <button className="big-btn primary" style={{ marginTop: 18 }} onClick={onRestart}>Create my profile</button>
          </React.Fragment>
        ) : (
          <React.Fragment>
            <h1>Your profile is active.</h1>
            <p className="lead-sum">This is the calm version of you others meet — no age, height, body type, income, scores, or public counts.</p>
          </React.Fragment>
        )}
      </section>

      {!guest && (
        <section className="block">
          <h2 className="block-h">Who can see you</h2>
          <div className="toggle-list card-list">
            <Toggle label="Take a break" hint="Hide me from the feed for now." checked={priv.paused} onChange={(v) => setPriv((p) => ({ ...p, paused: v }))} />
            <Toggle label="Women appreciating women only" checked={priv.womenOnly} onChange={(v) => setPriv((p) => ({ ...p, womenOnly: v }))} />
            <Toggle label="Hide me from people in my own country" checked={priv.hideCountry} onChange={(v) => setPriv((p) => ({ ...p, hideCountry: v }))} />
            <Toggle label="Only show me people outside my country" checked={priv.outsideOnly} onChange={(v) => setPriv((p) => ({ ...p, outsideOnly: v }))} />
            <Toggle label="Only connect with verified people" checked={priv.verifiedOnly} onChange={(v) => setPriv((p) => ({ ...p, verifiedOnly: v }))} />
          </div>
        </section>
      )}

      <section className="block">
        <h2 className="block-h">{guest ? 'Account' : 'Your data'}</h2>
        <div className="link-list">
          {!guest && <button className="link-row" onClick={onRestart}>Edit profile <Chevron /></button>}
          <button className="link-row">Download my data <Chevron /></button>
          <button className="link-row">Blocked people <Chevron /></button>
          {guest
            ? <button className="link-row" onClick={onRestart}>I already have an account <Chevron /></button>
            : <button className="link-row" onClick={onSignOut}>Sign out <Chevron /></button>}
          {!guest && <button className="link-row danger">Delete account <Chevron /></button>}
        </div>
      </section>
      <div className="screen-pad" />
    </div>
  );
}

function Chevron() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 5l7 7-7 7" /></svg>;
}

Object.assign(window, { OnboardingFlow, YouScreen, Toggle });
