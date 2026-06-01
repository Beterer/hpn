import { useReceivedAppreciation } from '../../lib/query/appreciation'

export function ReceivedView() {
  const received = useReceivedAppreciation(true)

  if (received.isLoading) {
    return <CenteredNote>Gathering what people have noticed…</CenteredNote>
  }

  if (received.isError) {
    return (
      <CenteredNote>
        <p className="text-rose-700">{received.error.message}</p>
        <button
          type="button"
          onClick={() => void received.refetch()}
          className="mt-3 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Try again
        </button>
      </CenteredNote>
    )
  }

  const summary = received.data
  if (!summary) {
    return null
  }

  const total = Number(summary.total)
  const categories = summary.categories ?? []
  const events = summary.events ?? []

  return (
    <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-7 px-4 py-8">
      <section className="border-b border-zinc-200 pb-6">
        <p className="text-sm font-semibold uppercase tracking-widest text-teal-700">Received</p>
        <h1 className="mt-3 text-3xl font-semibold text-zinc-950">{summary.headline}</h1>
        <p className="mt-3 max-w-xl text-sm leading-6 text-zinc-600">{summary.summary}</p>
        <p className="mt-4 text-sm font-medium text-zinc-800">
          {total === 0
            ? 'No appreciation received yet.'
            : `${total} appreciation ${total === 1 ? 'moment' : 'moments'} received privately.`}
        </p>
      </section>

      {categories.length === 0 ? (
        <section className="py-8 text-sm leading-6 text-zinc-600">
          As your profile is noticed, the words people choose will appear here with care.
        </section>
      ) : (
        <section className="grid gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">
            Ways people describe you
          </h2>
          <div className="grid gap-2 sm:grid-cols-2">
            {categories.map((category) => (
              <article
                key={category.categoryId}
                className="rounded-lg border border-zinc-200 bg-white p-4 shadow-sm"
              >
                <div className="flex items-start justify-between gap-3">
                  <h3 className="text-base font-semibold text-zinc-950">{category.label}</h3>
                  <span className="rounded-full bg-teal-50 px-2.5 py-1 text-xs font-semibold text-teal-800">
                    {category.count}
                  </span>
                </div>
                <p className="mt-2 text-sm leading-6 text-zinc-600">{category.phrasing}</p>
              </article>
            ))}
          </div>
        </section>
      )}

      {events.length > 0 && (
        <section className="grid gap-3">
          <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">Recent notes</h2>
          <ol className="grid gap-2">
            {events.map((event) => (
              <li key={event.id} className="rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
                <p className="text-sm font-medium text-zinc-900">{event.phrasing}</p>
                <p className="mt-1 text-xs text-zinc-500">{formatDate(event.createdAt)}</p>
              </li>
            ))}
          </ol>
        </section>
      )}
    </main>
  )
}

function CenteredNote({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">{children}</main>
  )
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))
}
