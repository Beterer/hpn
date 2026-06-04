import { useState } from 'react'
import { useGuestSession } from '../../lib/query/auth'
import { useFeedQueue } from '../../lib/query/feed'
import { MagicLinkForm } from '../auth/MagicLinkForm'
import { AppreciationChooser } from './AppreciationChooser'
import { FeedCard } from './FeedCard'

/**
 * The primary appreciation-gated screen (backbone §9.3). The only way to move
 * forward is a successful positive appreciation; there is no skip/dislike path.
 */
export function FeedScreen({ guest = false }: { guest?: boolean }) {
  const feed = useFeedQueue()
  const guestSession = useGuestSession()
  const [guestReactions, setGuestReactions] = useState(() => guestSession.reactionCount())
  const [lastNudgeDismissedAt, setLastNudgeDismissedAt] = useState(() => guestSession.lastNudgeDismissedAt())

  // A successful appreciation is the only thing that counts toward the signup nudge.
  const onUnlocked = () => {
    if (guest) {
      setGuestReactions(guestSession.recordReaction())
    }
    feed.advance()
  }

  // "Not for me" is the guest escape hatch: move past a profile without appreciating it
  // (the feed queue already remembers it as seen). It is not a reaction, so it does not
  // count toward the nudge.
  const skipProfile = () => {
    feed.advance()
  }

  const dismissNudge = () => {
    guestSession.dismissNudge(guestReactions)
    setLastNudgeDismissedAt(guestReactions)
  }

  if (feed.status === 'loading') {
    return <CenteredNote>Finding people to notice…</CenteredNote>
  }

  if (feed.status === 'error') {
    return (
      <CenteredNote>
        <p className="text-rose-700">{feed.error}</p>
        <button
          type="button"
          onClick={feed.reload}
          className="mt-3 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Try again
        </button>
      </CenteredNote>
    )
  }

  if (!feed.current) {
    return (
      <CenteredNote>
        <h2 className="text-xl font-semibold text-zinc-950">You're all caught up</h2>
        <p className="mt-2 max-w-sm text-sm text-zinc-600">
          There's no one new to notice right now. Check back a little later.
        </p>
        <button
          type="button"
          onClick={feed.reload}
          className="mt-4 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Refresh
        </button>
      </CenteredNote>
    )
  }

  return (
    <main className="mx-auto flex w-full max-w-md flex-col gap-4 px-4 py-8">
      <FeedCard key={feed.current.profileId} profile={feed.current} />
      {guest && (
        <button
          type="button"
          onClick={skipProfile}
          className="rounded-lg border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 hover:border-zinc-500"
        >
          Not for me
        </button>
      )}
      <AppreciationChooser key={feed.current.profileId} profile={feed.current} onUnlocked={onUnlocked} />
      {guest && guestSession.shouldShowNudge(guestReactions, lastNudgeDismissedAt) && (
        <div className="fixed inset-0 z-20 flex items-end bg-zinc-950/30 px-4 pb-4 sm:items-center sm:justify-center sm:p-6">
          <section className="w-full max-w-sm rounded-lg bg-white p-5 shadow-xl">
            <h2 className="text-lg font-semibold text-zinc-950">Keep your appreciations</h2>
            <p className="mt-2 text-sm leading-6 text-zinc-600">
              Sign in to carry this browsing session into your account.
            </p>
            <div className="mt-4">
              <MagicLinkNudge />
            </div>
            <button
              type="button"
              onClick={dismissNudge}
              className="mt-4 w-full rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50"
            >
              Keep browsing
            </button>
          </section>
        </div>
      )}
    </main>
  )
}

function MagicLinkNudge() {
  return <div className="[&_p]:text-sm"><MagicLinkForm /></div>
}

function CenteredNote({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">{children}</main>
  )
}
