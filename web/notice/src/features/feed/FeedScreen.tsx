import { useFeedQueue } from '../../lib/query/feed'
import { FeedCard } from './FeedCard'

/**
 * The primary appreciation-gated screen (backbone §9.3). M4 ships the card shell
 * consuming the prefetch queue; the appreciation chooser that advances the feed
 * lands in M5. Until then the surface stays calm and never offers a skip (§9.4).
 */
export function FeedScreen() {
  const feed = useFeedQueue()

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
      <FeedCard profile={feed.current} />
      <p className="text-center text-sm text-zinc-500">
        Appreciating someone is how you'll move to the next profile — that step arrives soon.
      </p>
    </main>
  )
}

function CenteredNote({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">{children}</main>
  )
}
