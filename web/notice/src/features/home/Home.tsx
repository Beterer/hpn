import { useState } from 'react'
import { useEnsureGuestSession, useMe } from '../../lib/query/auth'
import { MagicLinkForm } from '../auth/MagicLinkForm'
import { FeedScreen } from '../feed/FeedScreen'
import { AppShell } from '../shell/AppShell'

/**
 * The root screen: the guest feed for signed-out visitors, the app shell once a
 * session is established (backbone §9.3). /me decides which — the cookie is httpOnly, so
 * the server is the only source of truth about auth state (§9.2).
 */
export function Home() {
  const { data: me, isLoading } = useMe()

  if (isLoading) {
    return (
      <main className="flex min-h-full items-center justify-center">
        <p className="text-slate-500">Loading…</p>
      </main>
    )
  }

  if (me) {
    return <AppShell me={me} />
  }

  return <GuestFeedShell />
}

function GuestFeedShell() {
  const guest = useEnsureGuestSession()
  const [signInOpen, setSignInOpen] = useState(false)

  if (guest.isLoading) {
    return (
      <main className="flex min-h-full flex-col items-center justify-center px-6 text-center">
        <p className="text-slate-500">Opening the feed…</p>
      </main>
    )
  }

  if (guest.isError) {
    return (
      <main className="flex min-h-full flex-col items-center justify-center px-6 text-center">
        <p className="text-rose-700">{guest.error.message}</p>
        <button
          type="button"
          onClick={() => void guest.refetch()}
          className="mt-3 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Try again
        </button>
      </main>
    )
  }

  return (
    <div className="flex min-h-full flex-col bg-stone-50">
      <header className="flex items-center justify-between border-b border-zinc-200 bg-white px-4 py-4 sm:px-6">
        <span className="text-sm font-semibold uppercase tracking-widest text-teal-700">Notice</span>
        <button
          type="button"
          onClick={() => setSignInOpen(true)}
          className="text-sm font-medium text-zinc-600 hover:text-zinc-950"
        >
          Sign in
        </button>
      </header>
      <FeedScreen guest />

      {signInOpen && (
        <div className="fixed inset-0 z-30 flex items-end bg-zinc-950/30 px-4 pb-4 sm:items-center sm:justify-center sm:p-6">
          <section className="w-full max-w-sm rounded-lg bg-white p-5 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <h2 className="text-lg font-semibold text-zinc-950">Sign in to Notice</h2>
              <button
                type="button"
                onClick={() => setSignInOpen(false)}
                className="text-sm font-medium text-zinc-500 hover:text-zinc-900"
              >
                Close
              </button>
            </div>
            <div className="mt-4">
              <MagicLinkForm />
            </div>
          </section>
        </div>
      )}
    </div>
  )
}
