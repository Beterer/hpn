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
import { FingerprintScreen, LockedScreen, ReceivedScreen } from './Panels'
import { YouScreen } from './YouScreen'
import type { NavName } from './ui'

// Ensures the anonymous browser has a guest session cookie so the feed (which is
// guest-or-member) loads. Rendered only while signed out.
function GuestBoot() {
  useEnsureGuestSession()
  return null
}

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
  const [tab, setTab] = useState<NavName>(initialTab)
  const [authOpen, setAuthOpen] = useState(false)
  const profile = useMyProfile()
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

  // Member without an active profile → full-screen setup (no chrome).
  const needsOnboarding = !anon && (!profile.data || profile.data.status === 'draft')

  return (
    <div className="notice-root">
      <div className="app-root">
        {anon && <GuestBoot />}

        {!anon && profile.isLoading ? (
          <div className="centered-note">Loading…</div>
        ) : needsOnboarding ? (
          <OnboardingFlow profile={profile.data ?? null} onDone={() => setTab('feed')} />
        ) : (
          <>
            <AppHeader anon={anon} onNudge={() => setAuthOpen(true)} onGear={() => setTab('you')} />

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
                (anon ? <LockedScreen kind="received" onNudge={() => setAuthOpen(true)} /> : <ReceivedScreen />)}

              {tab === 'fingerprint' &&
                (anon ? <LockedScreen kind="fingerprint" onNudge={() => setAuthOpen(true)} /> : <FingerprintScreen />)}

              {tab === 'you' && (
                <YouScreen
                  mode={anon ? 'guest' : 'member'}
                  profile={profile.data ?? null}
                  onCreateProfile={() => setAuthOpen(true)}
                />
              )}
            </div>

            <BottomNav tab={tab} onTab={goTab} hasUnseen={hasUnseen && tab !== 'received'} />
          </>
        )}

        {authOpen && (
          <div className="auth-overlay">
            <AuthFlow onClose={() => setAuthOpen(false)} />
          </div>
        )}
      </div>
    </div>
  )
}
