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
  const toastedRef = useRef<Set<string>>(new Set())
  const [toast, setToast] = useState<NotificationItem | null>(null)

  const latest = summary.data?.latest ?? null
  const hasUnseen = Number(summary.data?.unseenCount ?? 0) > 0

  useEffect(() => {
    if (!latest || latest.seen || tab === 'received') return
    if (toastedRef.current.has(latest.id)) return
    toastedRef.current.add(latest.id)
    setToast(latest)
  }, [latest, tab])

  useEffect(() => {
    if (!anon && tab === 'received' && hasUnseen) {
      markSeen.mutate()
    }
    // The mutation is intentionally driven only by entering Received or unseen state changing.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, anon, hasUnseen])

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

            <BottomNav tab={tab} onTab={setTab} hasUnseen={hasUnseen && tab !== 'received'} />
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
