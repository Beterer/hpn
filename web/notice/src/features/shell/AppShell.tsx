import { useLocation, useNavigate } from 'react-router-dom'
import type { Me } from '../../lib/api/auth'
import { useLogout } from '../../lib/query/auth'
import { useMyProfile } from '../../lib/query/profile'
import { FeedScreen } from '../feed/FeedScreen'
import { OnboardingFlow } from '../profile/OnboardingFlow'
import { ProfileEditor } from '../profile/ProfileEditor'
import { ReceivedView } from '../received/ReceivedView'

type Tab = 'feed' | 'received' | 'profile'

const TAB_PATHS: Record<Tab, string> = {
  feed: '/',
  received: '/received',
  profile: '/profile',
}

function tabFromPath(pathname: string): Tab {
  if (pathname === '/received') {
    return 'received'
  }

  if (pathname === '/profile') {
    return 'profile'
  }

  return 'feed'
}

/**
 * Authenticated shell. Onboarding gates the rest; once a profile exists the shell
 * branches between the feed, received appreciation, and profile editor
 * (backbone §9.3). Copy stays appreciation-first, never competitive (§2).
 */
export function AppShell({ me }: { me: Me }) {
  const logout = useLogout()
  const profile = useMyProfile()
  const location = useLocation()
  const navigate = useNavigate()
  const tab = tabFromPath(location.pathname)

  const currentProfile = profile.data ?? null
  const showOnboarding = !currentProfile || currentProfile.status === 'draft'
  const showTab = (nextTab: Tab) => {
    void navigate(TAB_PATHS[nextTab])
  }

  return (
    <div className="flex min-h-full flex-col bg-stone-50">
      <header className="flex items-center justify-between border-b border-zinc-200 bg-white px-6 py-4">
        <span className="text-sm font-semibold uppercase tracking-widest text-teal-700">Notice</span>

        {!showOnboarding && (
          <nav className="flex items-center gap-1 rounded-lg bg-stone-100 p-1">
            <TabButton active={tab === 'feed'} onClick={() => showTab('feed')}>
              Feed
            </TabButton>
            <TabButton active={tab === 'received'} onClick={() => showTab('received')}>
              Received
            </TabButton>
            <TabButton active={tab === 'profile'} onClick={() => showTab('profile')}>
              Profile
            </TabButton>
          </nav>
        )}

        <button
          type="button"
          onClick={() => logout.mutate()}
          disabled={logout.isPending}
          className="text-sm font-medium text-zinc-600 hover:text-zinc-950 disabled:opacity-60"
        >
          {logout.isPending ? 'Signing out…' : 'Sign out'}
        </button>
      </header>

      {profile.isLoading && (
        <main className="flex flex-1 items-center justify-center">
          <p className="text-zinc-500">Loading…</p>
        </main>
      )}

      {profile.isError && (
        <main className="flex flex-1 items-center justify-center px-6">
          <p className="text-rose-700">Your profile could not be loaded.</p>
        </main>
      )}

      {!profile.isLoading && !profile.isError && showOnboarding && (
        <OnboardingFlow me={me} profile={currentProfile} />
      )}

      {!profile.isLoading && !profile.isError && !showOnboarding && currentProfile && (
        tab === 'feed' ? (
          <FeedScreen />
        ) : tab === 'received' ? (
          <ReceivedView />
        ) : (
          <ProfileEditor profile={currentProfile} />
        )
      )}
    </div>
  )
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-md px-3 py-1 text-sm font-medium transition ${
        active ? 'bg-white text-teal-800 shadow-sm' : 'text-zinc-600 hover:text-zinc-900'
      }`}
    >
      {children}
    </button>
  )
}
