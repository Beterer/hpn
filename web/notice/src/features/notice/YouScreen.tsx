import { useState } from 'react'
import type { Profile } from '../../lib/api/profile'
import { useAppreciationStyle } from '../../lib/query/appreciation'
import { useLogout } from '../../lib/query/auth'
import { useBlocks, useExportAccount, useRequestAccountDeletion, useUnblock, useUpdateVisibility } from '../../lib/query/settings'
import { OnboardingFlow } from './OnboardingFlow'

type Sub = 'main' | 'edit' | 'blocked' | 'style'

const ChevronRight = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 6l6 6-6 6" /></svg>
)

export function YouScreen({
  mode,
  profile,
  onCreateProfile,
}: {
  mode: 'guest' | 'member'
  profile: Profile | null
  onCreateProfile: () => void
}) {
  const [sub, setSub] = useState<Sub>('main')

  if (mode === 'guest' || !profile) {
    return (
      <div className="scroll-screen">
        <section className="lead">
          <p className="eyebrow">You</p>
          <h1>You're appreciating anonymously.</h1>
          <p className="lead-sum">
            Set up a profile to be noticed back. We never show age, height, body type, income, scores, or public counts.
          </p>
        </section>
        <button className="big-btn primary" onClick={onCreateProfile}>Create my profile</button>
        <div className="screen-pad" />
      </div>
    )
  }

  if (sub === 'edit') {
    return <OnboardingFlow profile={profile} onDone={() => setSub('main')} />
  }
  if (sub === 'blocked') {
    return <BlockedView onBack={() => setSub('main')} />
  }
  if (sub === 'style') {
    return <StyleView onBack={() => setSub('main')} />
  }

  return <YouMain profile={profile} onOpen={setSub} />
}

function YouMain({ profile, onOpen }: { profile: Profile; onOpen: (s: Sub) => void }) {
  const prefs = profile.visibilityPreferences
  const updateVisibility = useUpdateVisibility()
  const logout = useLogout()
  const exportData = useExportAccount()
  const deletion = useRequestAccountDeletion()
  const [confirmDelete, setConfirmDelete] = useState(false)

  const [vis, setVis] = useState({
    paused: prefs.paused,
    womenForWomen: prefs.womenForWomen,
    hideFromCountry: prefs.hideFromCountry,
    showOnlyOutsideCountry: prefs.showOnlyOutsideCountry,
    verifiedOnly: prefs.verifiedOnly,
  })

  const save = (patch: Partial<typeof vis>) => {
    const next = { ...vis, ...patch }
    setVis(next)
    updateVisibility.mutate({
      paused: next.paused,
      womenForWomen: next.womenForWomen,
      hideFromCountry: next.hideFromCountry,
      showOnlyOutsideCountry: next.showOnlyOutsideCountry,
      verifiedOnly: next.verifiedOnly,
      hiddenFromGuests: prefs.hiddenFromGuests,
      minDistanceKm: prefs.minDistanceKm == null ? null : Number(prefs.minDistanceKm),
    })
  }

  return (
    <div className="scroll-screen">
      <section className="lead">
        <p className="eyebrow">You</p>
        <h1>Your profile is active.</h1>
        <p className="lead-sum">
          You appear as <strong>{profile.displayName}</strong>. Notice never shows age, height, body type, income, scores, or public counts.
        </p>
      </section>

      <section className="block">
        <h2 className="block-h">Who can see you</h2>
        <div className="toggle-list">
          <Toggle label="Take a break" hint="Hide me from the feed for now." on={vis.paused} onToggle={() => save({ paused: !vis.paused })} />
          {profile.gender === 'woman' && (
            <Toggle label="Women appreciating women only" on={vis.womenForWomen} onToggle={() => save({ womenForWomen: !vis.womenForWomen })} />
          )}
          <Toggle label="Hide me from people in my own country" on={vis.hideFromCountry} onToggle={() => save({ hideFromCountry: !vis.hideFromCountry })} />
          <Toggle label="Only show me people outside my country" on={vis.showOnlyOutsideCountry} onToggle={() => save({ showOnlyOutsideCountry: !vis.showOnlyOutsideCountry })} />
          <Toggle label="Only connect with verified people" on={vis.verifiedOnly} onToggle={() => save({ verifiedOnly: !vis.verifiedOnly })} />
        </div>
        <p className="priv-note">Location stays coarse — rounded to roughly 11 km, broad distance bands only. Never your exact spot.</p>
      </section>

      <section className="block">
        <h2 className="block-h">Profile & data</h2>
        <div className="link-list">
          <button className="link-row" onClick={() => onOpen('edit')}>Edit profile <ChevronRight /></button>
          <button className="link-row" onClick={() => onOpen('style')}>How you appreciate <ChevronRight /></button>
          <button className="link-row" onClick={() => onOpen('blocked')}>Blocked people <ChevronRight /></button>
          <button className="link-row" disabled={exportData.isPending} onClick={() => exportData.mutate()}>
            {exportData.isPending ? 'Preparing…' : 'Download my data'} <ChevronRight />
          </button>
          <button className="link-row" disabled={logout.isPending} onClick={() => logout.mutate()}>
            {logout.isPending ? 'Signing out…' : 'Sign out'} <ChevronRight />
          </button>
        </div>
      </section>

      <section className="block">
        <h2 className="block-h">Delete account</h2>
        {deletion.isSuccess ? (
          <p className="priv-note">Your account is scheduled for deletion and you've been signed out. It's fully removed after a grace window.</p>
        ) : !confirmDelete ? (
          <div className="link-list">
            <button className="link-row danger" onClick={() => setConfirmDelete(true)}>Delete my account <ChevronRight /></button>
          </div>
        ) : (
          <div className="link-list">
            <button className="big-btn primary" style={{ background: 'var(--coral-deep)' }} disabled={deletion.isPending} onClick={() => deletion.mutate()}>
              {deletion.isPending ? 'Deleting…' : 'Yes, delete everything'}
            </button>
            <button className="welcome-skip" onClick={() => setConfirmDelete(false)}>Cancel</button>
          </div>
        )}
      </section>
      <div className="screen-pad" />
    </div>
  )
}

function BlockedView({ onBack }: { onBack: () => void }) {
  const blocks = useBlocks()
  const unblock = useUnblock()
  return (
    <div className="scroll-screen">
      <button className="welcome-skip" style={{ alignSelf: 'flex-start', marginLeft: -4 }} onClick={onBack}>← You</button>
      <section className="lead">
        <p className="eyebrow">Blocked</p>
        <h1>People you've blocked.</h1>
      </section>
      {blocks.isLoading && <p className="lead-sum">Loading…</p>}
      {blocks.data && blocks.data.length === 0 && <p className="lead-sum">You haven't blocked anyone.</p>}
      <div className="link-list">
        {(blocks.data ?? []).map((b) => (
          <div key={b.profileId} className="link-row">
            {b.displayName}
            <button className="welcome-skip" style={{ margin: 0 }} disabled={unblock.isPending} onClick={() => unblock.mutate(b.profileId)}>Unblock</button>
          </div>
        ))}
      </div>
      <div className="screen-pad" />
    </div>
  )
}

function StyleView({ onBack }: { onBack: () => void }) {
  const style = useAppreciationStyle()
  const data = style.data
  const ready = data?.status === 'ready'
  const categories = data?.categories ?? []

  return (
    <div className="scroll-screen">
      <button className="welcome-skip" style={{ alignSelf: 'flex-start', marginLeft: -4 }} onClick={onBack}>← You</button>
      <section className="lead">
        <p className="eyebrow">How you appreciate</p>
        <h1>{data?.headline ?? 'What you tend to notice'}</h1>
        <p className="lead-sum">{data?.summary ?? 'A private reading of the qualities you tend to notice in others.'}</p>
      </section>
      {style.isLoading && <p className="lead-sum">Loading…</p>}
      {!ready && !style.isLoading && <p className="lead-sum">{data?.summary}</p>}
      {ready && (
        <section className="block">
          <div className="dist-list">
            {[...categories].sort((a, b) => Number(b.share) - Number(a.share)).map((c) => (
              <div key={c.categoryId} className="dist-row">
                <span className="dist-label">{c.label}</span>
                <div className="fp-bar"><span style={{ width: `${Math.round(Number(c.share) * 100)}%`, background: 'var(--coral)' }} /></div>
                <span className="dist-pct">{Math.round(Number(c.share) * 100)}%</span>
              </div>
            ))}
          </div>
        </section>
      )}
      <div className="screen-pad" />
    </div>
  )
}

function Toggle({ label, hint, on, onToggle }: { label: string; hint?: string; on: boolean; onToggle: () => void }) {
  return (
    <button type="button" className={`toggle-row ${on ? 'on' : ''}`} onClick={onToggle} aria-pressed={on}>
      <span className="toggle-text">
        <span className="toggle-label">{label}</span>
        {hint && <span className="toggle-hint">{hint}</span>}
      </span>
      <span className={`switch ${on ? 'on' : ''}`}><span className="knob" /></span>
    </button>
  )
}
