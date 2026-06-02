import { useAppreciationStyle } from '../../lib/query/appreciation'

export function AppreciationStyleView() {
  const style = useAppreciationStyle()

  if (style.isLoading) {
    return <CenteredNote>Looking at what you tend to notice…</CenteredNote>
  }

  if (style.isError) {
    return (
      <CenteredNote>
        <p className="text-rose-700">{style.error.message}</p>
        <button
          type="button"
          onClick={() => void style.refetch()}
          className="mt-3 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-800 hover:border-teal-700 hover:text-teal-800"
        >
          Try again
        </button>
      </CenteredNote>
    )
  }

  const data = style.data
  if (!data) {
    return null
  }

  const categories = data.categories ?? []
  const noticed = categories.filter((category) => Number(category.count) > 0)
  const displayed = (noticed.length > 0 ? noticed : categories).slice(0, noticed.length > 0 ? noticed.length : 6)

  return (
    <main className="mx-auto flex w-full max-w-4xl flex-1 flex-col gap-7 px-4 py-8">
      <section className="border-b border-zinc-200 pb-6">
        <p className="text-sm font-semibold uppercase tracking-widest text-teal-700">Style</p>
        <h1 className="mt-3 text-3xl font-semibold text-zinc-950">{data.headline}</h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-zinc-600">{data.summary}</p>
        <p className="mt-4 text-sm font-medium text-zinc-800">
          {Number(data.total) === 0
            ? 'No appreciation given yet.'
            : `${data.total} appreciation ${Number(data.total) === 1 ? 'moment' : 'moments'} given.`}
        </p>
      </section>

      <section className="grid gap-3">
        <h2 className="text-sm font-semibold uppercase tracking-widest text-zinc-500">Your mix</h2>
        <div className="grid gap-3">
          {displayed.map((category) => (
            <article key={category.categoryId} className="rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 className="text-base font-semibold text-zinc-950">{category.label}</h3>
                  <p className="mt-1 text-sm leading-6 text-zinc-600">{category.insight}</p>
                </div>
                <span className="text-sm font-semibold text-teal-800">{toPercent(category.share)}</span>
              </div>

              <div className="mt-4 grid gap-2">
                <MixBar label="You" value={Number(category.share)} color="bg-teal-700" />
                <MixBar label="Notice" value={Number(category.platformShare)} color="bg-amber-600" />
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  )
}

function MixBar({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="grid grid-cols-[4.5rem_1fr_3rem] items-center gap-3">
      <span className="text-xs font-semibold uppercase tracking-widest text-zinc-500">{label}</span>
      <div className="h-2 overflow-hidden rounded-full bg-zinc-200">
        <div className={`h-full rounded-full ${color}`} style={{ width: toPercent(value) }} />
      </div>
      <span className="text-right text-xs font-semibold text-zinc-600">{toPercent(value)}</span>
    </div>
  )
}

function CenteredNote({ children }: { children: React.ReactNode }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">{children}</main>
  )
}

function toPercent(value: number | string): string {
  return `${Math.round(Number(value) * 100)}%`
}
