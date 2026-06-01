import type { Me } from '../../lib/api/auth'
import { useLogout } from '../../lib/query/auth'

/**
 * Minimal authenticated shell for M1 — enough to prove a held session. The real
 * feed/profile screens slot in here in later milestones (backbone §9.3). Copy
 * stays appreciation-first and never competitive (§2).
 */
export function AppShell({ me }: { me: Me }) {
  const logout = useLogout()

  return (
    <div className="flex min-h-full flex-col">
      <header className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
        <span className="text-sm font-semibold uppercase tracking-widest text-slate-400">Notice</span>
        <button
          type="button"
          onClick={() => logout.mutate()}
          disabled={logout.isPending}
          className="text-sm font-medium text-slate-600 hover:text-slate-900 disabled:opacity-60"
        >
          {logout.isPending ? 'Signing out…' : 'Sign out'}
        </button>
      </header>

      <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col justify-center gap-4 px-6 py-16">
        <h1 className="text-3xl font-semibold text-slate-900">You’re signed in</h1>
        <p className="text-slate-600">
          Signed in as <span className="font-medium text-slate-900">{me.user.email}</span>.
        </p>
        <p className="text-slate-500">
          Next, you’ll set up your profile so people can begin to notice what’s genuine about you.
        </p>
      </main>
    </div>
  )
}
