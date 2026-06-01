import type { Me } from '../../lib/api/auth'
import { useLogout } from '../../lib/query/auth'
import { useMyProfile } from '../../lib/query/profile'
import { OnboardingFlow } from '../profile/OnboardingFlow'
import { ProfileEditor } from '../profile/ProfileEditor'

/**
 * Minimal authenticated shell for M1 — enough to prove a held session. The real
 * feed/profile screens slot in here in later milestones (backbone §9.3). Copy
 * stays appreciation-first and never competitive (§2).
 */
export function AppShell({ me }: { me: Me }) {
  const logout = useLogout()
  const profile = useMyProfile()

  const currentProfile = profile.data ?? null
  const showOnboarding = !currentProfile || currentProfile.status === 'draft'

  return (
    <div className="flex min-h-full flex-col bg-stone-50">
      <header className="flex items-center justify-between border-b border-zinc-200 bg-white px-6 py-4">
        <span className="text-sm font-semibold uppercase tracking-widest text-teal-700">Notice</span>
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
        <ProfileEditor profile={currentProfile} />
      )}
    </div>
  )
}
