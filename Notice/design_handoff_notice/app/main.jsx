// main.jsx — App root: device scaling, navigation, tweaks.
const { useState: useStateM, useEffect: useEffectM, useRef: useRefM, useCallback: useCallbackM } = React;

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "reward": "both",
  "flow": "tray",
  "cardRadius": 24,
  "density": "regular"
}/*EDITMODE-END*/;

const LS = {
  get account() { return localStorage.getItem('notice.account') || 'anon'; },
  set account(v) { localStorage.setItem('notice.account', v); },
  get maturity() { return localStorage.getItem('notice.maturity') || 'established'; },
  set maturity(v) { localStorage.setItem('notice.maturity', v); },
  get firstSeen() { return localStorage.getItem('notice.firstSeen') === '1'; },
  set firstSeen(v) { localStorage.setItem('notice.firstSeen', v ? '1' : '0'); },
  get tab() { return localStorage.getItem('notice.tab') || 'feed'; },
  set tab(v) { localStorage.setItem('notice.tab', v); },
};

function useScale(w, h, pad) {
  const [scale, setScale] = useStateM(1);
  useEffectM(() => {
    const fit = () => {
      const s = Math.min((window.innerWidth - pad) / w, (window.innerHeight - pad) / h, 1.08);
      setScale(s > 0 ? s : 1);
    };
    fit();
    window.addEventListener('resize', fit);
    return () => window.removeEventListener('resize', fit);
  }, [w, h, pad]);
  return scale;
}

function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);
  const [account, setAccount] = useStateM(LS.account);
  const [maturity, setMaturity] = useStateM(LS.maturity); // 'fresh' | 'established'
  const [firstSeen, setFirstSeen] = useStateM(LS.firstSeen);
  const [tab, setTabState] = useStateM(LS.tab);
  const [authOpen, setAuthOpen] = useStateM(false);
  const [toast, setToast] = useStateM(null);
  const [reveal, setReveal] = useStateM(false);
  const toastArmed = useRefM(false);
  const scale = useScale(402, 874, 36);

  const anon = account === 'anon';
  const fresh = !anon && maturity === 'fresh';
  const setTab = (v) => { setTabState(v); LS.tab = v; if (v !== 'received') setReveal(false); };

  const enter = (mode, mat = 'established') => {
    setAccount(mode); LS.account = mode;
    setMaturity(mat); LS.maturity = mat;
    if (mat === 'fresh') { setFirstSeen(false); LS.firstSeen = false; toastArmed.current = false; }
    setAuthOpen(false); setTab('feed');
  };
  const openAuth = () => setAuthOpen(true);
  const signOut = () => { setAccount('anon'); LS.account = 'anon'; setTab('feed'); };

  // open the first appreciation from the toast
  const openFirst = () => {
    setToast(null);
    setFirstSeen(true); LS.firstSeen = true;
    setReveal(true);
    setTabState('received'); LS.tab = 'received';
  };

  // demo jump between states
  const setDemo = (v) => {
    if (v === 'anon') { setAccount('anon'); LS.account = 'anon'; setTab('feed'); setToast(null); return; }
    if (v === 'new') {
      setAccount('member'); LS.account = 'member';
      setMaturity('fresh'); LS.maturity = 'fresh';
      setFirstSeen(false); LS.firstSeen = false;
      toastArmed.current = false; setToast(null); setReveal(false);
      setTabState('feed'); LS.tab = 'feed';
      return;
    }
    // established
    setAccount('member'); LS.account = 'member';
    setMaturity('established'); LS.maturity = 'established';
    setToast(null); setTabState('feed'); LS.tab = 'feed';
  };
  const demoState = anon ? 'anon' : maturity === 'fresh' ? 'new' : 'established';

  // arm the incoming appreciation once, a few seconds into the feed
  useEffectM(() => {
    if (anon || !fresh || firstSeen || toastArmed.current) return;
    if (tab !== 'feed') return;
    toastArmed.current = true;
    const id = setTimeout(() => setToast(FIRST_APPRECIATION), 2800);
    return () => clearTimeout(id);
  }, [anon, fresh, firstSeen, tab]);

  const locked = anon && (tab === 'received' || tab === 'fingerprint');

  return (
    <React.Fragment>
      <div className="stage">
        <div className="scaler" style={{ transform: `scale(${scale})` }}>
          <IOSDevice width={402} height={874}>
            <div className="app-root">
              <AppHeader anon={anon} onNudge={openAuth} onGear={() => setTab('you')} />
              <div className="app-content">
                {tab === 'feed' && <FeedScreen tweaks={t} />}
                {locked && <LockedScreen kind={tab} onNudge={openAuth} />}
                {!locked && tab === 'received' && (
                  fresh
                    ? (firstSeen ? <ReceivedEarly first={FIRST_APPRECIATION} reveal={reveal} /> : <ReceivedEmpty />)
                    : <ReceivedScreen />
                )}
                {!locked && tab === 'fingerprint' && (
                  fresh ? <FingerprintNascent seen={firstSeen} /> : <FingerprintScreen />
                )}
                {tab === 'you' && <YouScreen mode={anon ? 'guest' : 'member'} onRestart={openAuth} onSignOut={signOut} />}
              </div>
              <BottomNav tab={tab} setTab={setTab} />

              {toast && (
                <IncomingToast data={toast} onOpen={openFirst} onDismiss={() => setToast(null)} />
              )}

              {authOpen && (
                <div className="auth-overlay">
                  <OnboardingFlow onEnter={enter} onClose={() => setAuthOpen(false)} />
                </div>
              )}
            </div>
          </IOSDevice>
        </div>
      </div>

      <TweaksPanel>
        <TweakSection label="The reward moment" />
        <TweakRadio label="Reward style" value={t.reward} options={['glow', 'confetti', 'both']} onChange={(v) => setTweak('reward', v)} />
        <TweakSection label="Reaction flow" />
        <TweakRadio label="Pop-up style" value={t.flow} options={['tray', 'sheet']} onChange={(v) => setTweak('flow', v)} />
        <TweakSection label="Card" />
        <TweakSlider label="Corner radius" value={t.cardRadius} min={8} max={34} unit="px" onChange={(v) => setTweak('cardRadius', v)} />
        <TweakRadio label="Immersiveness" value={t.density} options={['compact', 'regular', 'comfy']} onChange={(v) => setTweak('density', v)} />
        <TweakSection label="Demo state" />
        <TweakRadio label="Who am I" value={demoState} options={['anon', 'new', 'established']} onChange={setDemo} />
        <TweakButton label="Replay first-time moment" onClick={() => setDemo('new')} />
      </TweaksPanel>
    </React.Fragment>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
