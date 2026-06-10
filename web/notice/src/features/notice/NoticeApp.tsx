import { useEffect, useRef, useState } from 'react'
import type { Me } from '../../lib/api/auth'
import type { NotificationItem } from '../../lib/api/notifications'
import { useEnsureGuestSession } from '../../lib/query/auth'
import { useMarkNotificationsSeen, useNotificationSummary } from '../../lib/query/notifications'
import { useMyProfile } from '../../lib/query/profile'
import { AppHeader, BottomNav } from './Chrome'
import { AuthFlow } from './AuthFlow'
import { FeedScreen } from './FeedScreen'
import { IncomingToast } from './IncomingToast'
import { OnboardingFlow } from './OnboardingFlow'
import { clearDeferred, readDeferred, writeDeferred } from './onboardingDeferral'
import { FingerprintScreen, LockedScreen, ReceivedScreen } from './Panels'
import { YouScreen } from './YouScreen'
import type { NavName } from './ui'

function initialTab(): NavName {
  const path = window.location.pathname
  if (path.startsWith('/received')) return 'received'
  if (path.startsWith('/me/fingerprint')) return 'fingerprint'
  if (path.startsWith('/you') || path.startsWith('/settings') || path.startsWith('/profile')) return 'you'
  return 'feed'
}

// The toast is a one-shot per notification. Persist the ids we've already shown so
// a page refresh doesn't re-pop a toast for a notification that is still unseen
// (the dot, not the toast, is what persists until the user opens Received).
const TOASTED_KEY = 'notice.toastedNotificationIds'
function loadToastedIds(): Set<string> {
  try {
    return new Set(JSON.parse(localStorage.getItem(TOASTED_KEY) ?? '[]') as string[])
  } catch {
    return new Set()
  }
}
function rememberToastedId(id: string) {
  try {
    const ids = [...loadToastedIds(), id].slice(-50) // bound the stored history
    localStorage.setItem(TOASTED_KEY, JSON.stringify(ids))
  } catch {
    // localStorage unavailable (private mode / quota) — degrade to per-session.
  }
}

/**
 * Root of the Notice redesign (ADR-025): the app-root column with header, the
 * active tab, and the bottom nav. Branches on account (anon vs member) and, for
 * members, gates the shell behind profile setup. Members poll for received
 * appreciations to drive the Received dot and incoming-appreciation toast.
 */
export function NoticeApp({ me }: { me: Me | null }) {
  const anon = me === null
  const accountId = me?.user.id ?? null
  const [tab, setTab] = useState<NavName>(initialTab)
  const [authOpen, setAuthOpen] = useState(false)
  const [authInitial, setAuthInitial] = useState<'create' | 'signin'>('create')
  // "Browse for now" lets an incomplete member out of the setup wall into the shell.
  // Persisted per account so a refresh doesn't re-trap them; re-read when the signed-in
  // account changes (React's adjust-state-during-render pattern) so a different member on
  // the same browser still gets their own first-run onboarding.
  const [deferred, setDeferred] = useState(() => readDeferred(accountId))
  const [deferredAccountId, setDeferredAccountId] = useState(accountId)
  if (deferredAccountId !== accountId) {
    setDeferredAccountId(accountId)
    setDeferred(readDeferred(accountId))
  }

  const openAuth = (mode: 'create' | 'signin' = 'create') => {
    setAuthInitial(mode)
    setAuthOpen(true)
  }
  const guestSession = useEnsureGuestSession(anon)
  const profile = useMyProfile(!anon)
  const summary = useNotificationSummary(!anon)
  const markSeen = useMarkNotificationsSeen()
  const toastedRef = useRef<Set<string> | null>(null)
  toastedRef.current ??= loadToastedIds()
  const [toast, setToast] = useState<NotificationItem | null>(null)

  const latest = summary.data?.latest ?? null
  const hasUnseen = Number(summary.data?.unseenCount ?? 0) > 0

  useEffect(() => {
    if (!latest || latest.seen || tab === 'received') return
    const toasted = toastedRef.current!
    if (toasted.has(latest.id)) return
    toasted.add(latest.id)
    rememberToastedId(latest.id)
    setToast(latest)
  }, [latest, tab])

  useEffect(() => {
    if (!anon && tab === 'received' && hasUnseen) {
      markSeen.mutate()
    }
    // The mutation is intentionally driven only by entering Received or unseen state changing.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, anon, hasUnseen])

  // Navigating to Received acknowledges the toast just like tapping it would.
  const goTab = (next: NavName) => {
    if (next === 'received') setToast(null)
    setTab(next)
  }

  // Member without an active profile. They land in full-screen setup, but can defer it
  // ("Browse for now") to use the shell, then resume from the header / You / locked tabs.
  const noProfile = !anon && (!profile.data || profile.data.status === 'draft')
  const showOnboarding = noProfile && !deferred

  const deferOnboarding = () => {
    writeDeferred(accountId)
    setDeferred(true)
    setTab('feed')
  }
  const resumeOnboarding = () => {
    clearDeferred(accountId)
    setDeferred(false)
    setTab('feed')
  }
  const finishOnboarding = () => {
    clearDeferred(accountId)
    setTab('feed')
  }

  return (
    <div className="notice-root">
      <div className="app-root">
        {anon && guestSession.isLoading ? (
          <div className="centered-note">Opening Notice…</div>
        ) : anon && guestSession.isError ? (
          <div className="centered-note">
            <p>{guestSession.error.message}</p>
            <button className="big-btn ghost" style={{ maxWidth: 200 }} onClick={() => void guestSession.refetch()}>
              Try again
            </button>
          </div>
        ) : !anon && profile.isLoading ? (
          <div className="centered-note">Loading…</div>
        ) : showOnboarding ? (
          <OnboardingFlow profile={profile.data ?? null} onDone={finishOnboarding} onDefer={deferOnboarding} />
        ) : (
          <>
            <AppHeader
              anon={anon}
              incomplete={noProfile}
              onNudge={() => openAuth()}
              onResume={resumeOnboarding}
              onGear={() => setTab('you')}
            />

            {toast && (
              <IncomingToast
                item={toast}
                onOpen={() => {
                  setToast(null)
                  setTab('received')
                }}
                onDismiss={() => setToast(null)}
              />
            )}

            <div className="app-content">
              {tab === 'feed' && <FeedScreen />}

              {tab === 'received' &&
                (anon || noProfile ? (
                  <LockedScreen kind="received" incomplete={noProfile} onNudge={noProfile ? resumeOnboarding : () => openAuth()} />
                ) : (
                  <ReceivedScreen />
                ))}

              {tab === 'fingerprint' &&
                (anon || noProfile ? (
                  <LockedScreen kind="fingerprint" incomplete={noProfile} onNudge={noProfile ? resumeOnboarding : () => openAuth()} />
                ) : (
                  <FingerprintScreen />
                ))}

              {tab === 'you' && (
                <YouScreen
                  mode={anon ? 'guest' : 'member'}
                  incomplete={noProfile}
                  profile={profile.data ?? null}
                  onCreateProfile={noProfile ? resumeOnboarding : () => openAuth()}
                  onSignIn={() => openAuth('signin')}
                />
              )}
            </div>

            <BottomNav tab={tab} onTab={goTab} hasUnseen={hasUnseen && tab !== 'received'} />
          </>
        )}

        {authOpen && (
          <div className="auth-overlay">
            <AuthFlow onClose={() => setAuthOpen(false)} initialMode={authInitial} />
          </div>
        )}
      </div>
    </div>
  )
}
